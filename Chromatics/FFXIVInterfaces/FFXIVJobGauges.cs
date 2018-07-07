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
        public void ImplementJobGauges(List<StatusItem> statEffects, Color baseColor)
        { 
            if (ChromaticsSettings.ChromaticsSettingsJobGaugeToggle)
            {
                switch (_playerInfo.Job)
                {
                    case Actor.Job.WAR:
                        var burstwarcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWARWrathBurst);
                        var maxwarcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWARWrathMax);
                        var negwarcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWARNegative);
                        var wrath = Cooldowns.Wrath;
                        var polWrath = (wrath - 0) * (50 - 0) / (100 - 0) + 0;

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
                                ToggleGlobalFlash3(false);

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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstwarcol, false, false);
                                }
                            }
                            else if (polWrath <= 40 && polWrath > 30)
                            {
                                ToggleGlobalFlash3(false);

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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstwarcol, false, false);
                                }
                            }
                            else if (polWrath <= 30 && polWrath > 20)
                            {
                                ToggleGlobalFlash3(false);

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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstwarcol, false, false);
                                }
                            }
                            else if (polWrath <= 20 && polWrath > 10)
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

                                GlobalApplyMapKeyLighting("Num1", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstwarcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstwarcol, false, false);
                                }
                            }
                            else if (polWrath <= 10 && polWrath > 0)
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

                                GlobalApplyMapKeyLighting("Num1", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstwarcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstwarcol, false, false);
                                }
                            }
                            else if (polWrath == 0)
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", negwarcol, false, false);
                                }
                            }
                            else
                            {
                                ToggleGlobalFlash3(false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", negwarcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", negwarcol, false, false);
                                }
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

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", negwarcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", negwarcol, false, false);
                            }
                        }

                        break;
                    case Actor.Job.PLD:
                        //Paladin
                        var negpldcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobPLDNegative);
                        var shieldcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobPLDShieldOath);
                        var swordcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobPLDSwordOath);

                        var oathgauge = Cooldowns.OathGauge;
                        var oathPol = (oathgauge - 0) * (50 - 0) / (100 - 0) + 0;

                        if (statEffects.Find(i => i.StatusName == "Shield Oath") != null)
                        {
                            GlobalApplyMapKeyLighting("NumSubtract", shieldcol, false);
                            GlobalApplyMapKeyLighting("NumAdd", shieldcol, false);
                            GlobalApplyMapKeyLighting("NumEnter", shieldcol, false);

                            if (oathPol <= 50 && oathPol > 40)
                            {
                                GlobalApplyMapKeyLighting("NumLock", shieldcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", shieldcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", shieldcol, false);

                                GlobalApplyMapKeyLighting("Num7", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num8", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num9", shieldcol, false);

                                GlobalApplyMapKeyLighting("Num4", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num5", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num6", shieldcol, false);

                                GlobalApplyMapKeyLighting("Num1", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num2", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num3", shieldcol, false);

                                GlobalApplyMapKeyLighting("Num0", shieldcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", shieldcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", shieldcol, false, false);
                                }
                            }
                            else if (oathPol <= 40 && oathPol > 30)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num7", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num8", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num9", shieldcol, false);

                                GlobalApplyMapKeyLighting("Num4", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num5", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num6", shieldcol, false);

                                GlobalApplyMapKeyLighting("Num1", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num2", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num3", shieldcol, false);

                                GlobalApplyMapKeyLighting("Num0", shieldcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", shieldcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", shieldcol, false, false);
                                }
                            }
                            else if (oathPol <= 30 && oathPol > 20)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num4", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num5", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num6", shieldcol, false);

                                GlobalApplyMapKeyLighting("Num1", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num2", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num3", shieldcol, false);

                                GlobalApplyMapKeyLighting("Num0", shieldcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", shieldcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", shieldcol, false, false);
                                }
                            }
                            else if (oathPol <= 20 && oathPol > 10)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num1", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num2", shieldcol, false);
                                GlobalApplyMapKeyLighting("Num3", shieldcol, false);

                                GlobalApplyMapKeyLighting("Num0", shieldcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", shieldcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", shieldcol, false, false);
                                }
                            }
                            else if (oathPol <= 10 && oathPol > 0)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num1", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num2", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num3", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num0", shieldcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", shieldcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", shieldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", shieldcol, false, false);
                                }
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num1", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num2", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num3", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num0", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", negpldcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", negpldcol, false, false);
                                }
                            }
                        }
                        else if (statEffects.Find(i => i.StatusName == "Sword Oath") != null)
                        {
                            GlobalApplyMapKeyLighting("NumSubtract", swordcol, false);
                            GlobalApplyMapKeyLighting("NumAdd", swordcol, false);
                            GlobalApplyMapKeyLighting("NumEnter", swordcol, false);

                            if (oathPol <= 50 && oathPol > 40)
                            {
                                GlobalApplyMapKeyLighting("NumLock", swordcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", swordcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", swordcol, false);

                                GlobalApplyMapKeyLighting("Num7", swordcol, false);
                                GlobalApplyMapKeyLighting("Num8", swordcol, false);
                                GlobalApplyMapKeyLighting("Num9", swordcol, false);

                                GlobalApplyMapKeyLighting("Num4", swordcol, false);
                                GlobalApplyMapKeyLighting("Num5", swordcol, false);
                                GlobalApplyMapKeyLighting("Num6", swordcol, false);

                                GlobalApplyMapKeyLighting("Num1", swordcol, false);
                                GlobalApplyMapKeyLighting("Num2", swordcol, false);
                                GlobalApplyMapKeyLighting("Num3", swordcol, false);

                                GlobalApplyMapKeyLighting("Num0", swordcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", swordcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", swordcol, false, false);
                                }
                            }
                            else if (oathPol <= 40 && oathPol > 30)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num7", swordcol, false);
                                GlobalApplyMapKeyLighting("Num8", swordcol, false);
                                GlobalApplyMapKeyLighting("Num9", swordcol, false);

                                GlobalApplyMapKeyLighting("Num4", swordcol, false);
                                GlobalApplyMapKeyLighting("Num5", swordcol, false);
                                GlobalApplyMapKeyLighting("Num6", swordcol, false);

                                GlobalApplyMapKeyLighting("Num1", swordcol, false);
                                GlobalApplyMapKeyLighting("Num2", swordcol, false);
                                GlobalApplyMapKeyLighting("Num3", swordcol, false);

                                GlobalApplyMapKeyLighting("Num0", swordcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", swordcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", swordcol, false, false);
                                }
                            }
                            else if (oathPol <= 30 && oathPol > 20)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num4", swordcol, false);
                                GlobalApplyMapKeyLighting("Num5", swordcol, false);
                                GlobalApplyMapKeyLighting("Num6", swordcol, false);

                                GlobalApplyMapKeyLighting("Num1", swordcol, false);
                                GlobalApplyMapKeyLighting("Num2", swordcol, false);
                                GlobalApplyMapKeyLighting("Num3", swordcol, false);

                                GlobalApplyMapKeyLighting("Num0", swordcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", swordcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", swordcol, false, false);
                                }
                            }
                            else if (oathPol <= 20 && oathPol > 10)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num1", swordcol, false);
                                GlobalApplyMapKeyLighting("Num2", swordcol, false);
                                GlobalApplyMapKeyLighting("Num3", swordcol, false);

                                GlobalApplyMapKeyLighting("Num0", swordcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", swordcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", swordcol, false, false);
                                }
                            }
                            else if (oathPol <= 10 && oathPol > 0)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num1", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num2", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num3", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num0", swordcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", swordcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", swordcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", swordcol, false, false);
                                }
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num1", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num2", negpldcol, false);
                                GlobalApplyMapKeyLighting("Num3", negpldcol, false);

                                GlobalApplyMapKeyLighting("Num0", negpldcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", negpldcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", negpldcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", negpldcol, false, false);
                                }
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                            GlobalApplyMapKeyLighting("NumSubtract", negpldcol, false);
                            GlobalApplyMapKeyLighting("NumAdd", negpldcol, false);
                            GlobalApplyMapKeyLighting("NumEnter", negpldcol, false);

                            GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                            GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                            GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                            GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                            GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                            GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                            GlobalApplyMapKeyLighting("Num1", negpldcol, false);
                            GlobalApplyMapKeyLighting("Num2", negpldcol, false);
                            GlobalApplyMapKeyLighting("Num3", negpldcol, false);

                            GlobalApplyMapKeyLighting("Num0", negpldcol, false);
                            GlobalApplyMapKeyLighting("NumDecimal", negpldcol, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", negpldcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", negpldcol, false, false);
                            }
                        }

                        break;
                    case Actor.Job.MNK:
                        var greased = Cooldowns.GreasedLightningStacks;
                        var greaseRemaining = Cooldowns.GreasedLightningTimeRemaining;
                        var burstmnkcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMNKGreased);
                        var burstmnkempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMNKNegative);

                        if (greased > 0)
                        {
                            if (greaseRemaining > 0 && greaseRemaining <= 5)
                            {
                                ToggleGlobalFlash3(true);
                                GlobalFlash3(burstmnkcol, 150);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstmnkempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstmnkempty, false, false);
                                }
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

                                        if (_LightbarMode == LightbarMode.JobGauge)
                                        {
                                            GlobalApplyMapLightbarLighting("Lightbar19", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1", burstmnkcol, false, false);
                                        }
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

                                        if (_LightbarMode == LightbarMode.JobGauge)
                                        {
                                            GlobalApplyMapLightbarLighting("Lightbar19", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1", burstmnkcol, false, false);
                                        }
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

                                        if (_LightbarMode == LightbarMode.JobGauge)
                                        {
                                            GlobalApplyMapLightbarLighting("Lightbar19", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9", burstmnkempty, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2", burstmnkcol, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1", burstmnkcol, false, false);
                                        }
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

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", burstmnkempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", burstmnkempty, false, false);
                            }
                        }

                        break;
                    case Actor.Job.DRG:

                        var burstdrgcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGBloodDragon);
                        var negdrgcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGNegative);
                        var bloodremain = Cooldowns.BloodOfTheDragonTimeRemaining;
                        var polBlood = (bloodremain - 0) * (50 - 0) / (30 - 0) + 0;

                        if (bloodremain > 0)
                        {
                            if (polBlood <= 50 && polBlood > 40)
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstdrgcol, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstdrgcol, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstdrgcol, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstdrgcol, false, false);
                                }
                            }
                            else if (polBlood <= 10 && polBlood > 0)
                            {
                                //Flash
                                ToggleGlobalFlash3(true);
                                GlobalFlash3(burstdrgcol, 150);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", negdrgcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", negdrgcol, false, false);
                                }
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

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", negdrgcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", negdrgcol, false, false);
                            }
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
                            var polSong = (songremain - 0) * (50 - 0) / (30 - 0) + 0;

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
                            }

                            if (polSong <= 50 && polSong > 40)
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstcol, false, false);
                                }
                            }
                            else if (polSong <= 40 && polSong > 30)
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstcol, false, false);
                                }
                            }
                            else if (polSong <= 30 && polSong > 20)
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstcol, false, false);
                                }
                            }
                            else if (polSong <= 20 && polSong > 10)
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", burstcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", burstcol, false, false);
                                }
                            }
                            else if (polSong <= 10 && polSong > 0)
                            {
                                //Flash
                                ToggleGlobalFlash3(true);
                                GlobalFlash3(burstcol, 150);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", negcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", negcol, false, false);
                                }
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
                                GlobalApplyMapLightbarLighting("Lightbar19", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", negcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", negcol, false, false);
                            }
                        }
                        break;
                    case Actor.Job.WHM:
                        //White Mage
                        var negwhmcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWHMNegative);
                        var flowercol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWHMFlowerPetal);
                        var cureproc = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWHMFreecure);

                        var petalcount = Cooldowns.FlowerPetals;

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

                        switch (petalcount)
                        {
                            case 3:
                                GlobalApplyMapKeyLighting("Num9", flowercol, false);
                                GlobalApplyMapKeyLighting("Num6", flowercol, false);
                                GlobalApplyMapKeyLighting("Num3", flowercol, false);

                                GlobalApplyMapKeyLighting("Num8", flowercol, false);
                                GlobalApplyMapKeyLighting("Num5", flowercol, false);
                                GlobalApplyMapKeyLighting("Num2", flowercol, false);

                                GlobalApplyMapKeyLighting("Num7", flowercol, false);
                                GlobalApplyMapKeyLighting("Num4", flowercol, false);
                                GlobalApplyMapKeyLighting("Num1", flowercol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", flowercol, false, false);
                                }
                                break;
                            case 2:
                                GlobalApplyMapKeyLighting("Num9", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num6", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num3", negwhmcol, false);

                                GlobalApplyMapKeyLighting("Num8", flowercol, false);
                                GlobalApplyMapKeyLighting("Num5", flowercol, false);
                                GlobalApplyMapKeyLighting("Num2", flowercol, false);

                                GlobalApplyMapKeyLighting("Num7", flowercol, false);
                                GlobalApplyMapKeyLighting("Num4", flowercol, false);
                                GlobalApplyMapKeyLighting("Num1", flowercol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", flowercol, false, false);
                                }
                                break;
                            case 1:
                                GlobalApplyMapKeyLighting("Num9", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num6", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num3", negwhmcol, false);

                                GlobalApplyMapKeyLighting("Num8", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num5", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num2", negwhmcol, false);

                                GlobalApplyMapKeyLighting("Num7", flowercol, false);
                                GlobalApplyMapKeyLighting("Num4", flowercol, false);
                                GlobalApplyMapKeyLighting("Num1", flowercol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", flowercol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", flowercol, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", negwhmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", negwhmcol, false, false);
                                }
                                break;
                        }


                        break;
                    case Actor.Job.BLM:
                        //Black Mage

                        var negblmcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMNegative);
                        var firecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMAstralFire);
                        var icecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMUmbralIce);
                        var heartcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMUmbralHeart);
                        var enochcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMEnochian);

                        var firestacks = Cooldowns.AstralFire;
                        var icestacks = Cooldowns.UmbralIce;
                        var heartstacks = Cooldowns.UmbralHearts;

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
                            var enochtime = (Cooldowns.EnochianTimeRemaining - 0) * (40 - 0) / (30 - 0) + 0;

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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", enochcol, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", enochcol, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", enochcol, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", enochcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", enochcol, false, false);
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
                                    GlobalApplyMapLightbarLighting("Lightbar19", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", negblmcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", negblmcol, false, false);
                                }
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
                                GlobalApplyMapLightbarLighting("Lightbar19", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", negblmcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", negblmcol, false, false);
                            }
                        }

                        break;
                    case Actor.Job.SMN:
                        var aetherflowsmn = Cooldowns.AetherflowCount;

                        var burstsmncol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSMNAetherflow);
                        var burstsmnempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSMNNegative);

                        if (aetherflowsmn > 0)
                        {
                            switch (aetherflowsmn)
                            {
                                case 3:
                                    GlobalApplyMapKeyLighting("Num9", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num6", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num3", burstsmncol, false);

                                    GlobalApplyMapKeyLighting("Num8", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num5", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num2", burstsmncol, false);

                                    GlobalApplyMapKeyLighting("Num7", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num4", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num1", burstsmncol, false);

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", burstsmncol, false, false);
                                    }
                                    break;
                                case 2:
                                    GlobalApplyMapKeyLighting("Num9", burstsmnempty, false);
                                    GlobalApplyMapKeyLighting("Num6", burstsmnempty, false);
                                    GlobalApplyMapKeyLighting("Num3", burstsmnempty, false);

                                    GlobalApplyMapKeyLighting("Num8", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num5", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num2", burstsmncol, false);

                                    GlobalApplyMapKeyLighting("Num7", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num4", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num1", burstsmncol, false);

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", burstsmncol, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", burstsmnempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", burstsmncol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", burstsmncol, false, false);
                                    }
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

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", burstsmnempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", burstsmnempty, false, false);
                            }
                        }

                        break;
                    case Actor.Job.SCH:

                        var aetherflowsch = Cooldowns.AetherflowCount;

                        var burstschcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSCHAetherflow);
                        var burstschempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSCHNegative);

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
                        var polHuton = (hutonremain - 0) * (50 - 0) / (70 - 0) + 0;

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

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", hutoncol, false, false);
                            }
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

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", hutoncol, false, false);
                            }
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

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", hutoncol, false, false);
                            }
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

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", hutoncol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", hutoncol, false, false);
                            }
                        }
                        else if (polHuton <= 10 && polHuton > 0)
                        {
                            //Flash
                            ToggleGlobalFlash3(true);
                            GlobalFlash3(hutoncol, 150);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", negnincol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", negnincol, false, false);
                            }
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
                        var darksidecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRKDarkside);

                        var bloodgauge = Cooldowns.BloodGauge;
                        var bloodPol = (bloodgauge - 0) * (50 - 0) / (100 - 0) + 0;

                        if (statEffects.Find(i => i.StatusName == "Darkside") != null)
                        {
                            GlobalApplyMapKeyLighting("NumSubtract", darksidecol, false);
                            GlobalApplyMapKeyLighting("NumAdd", darksidecol, false);
                            GlobalApplyMapKeyLighting("NumEnter", darksidecol, false);
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("NumSubtract", negdrkcol, false);
                            GlobalApplyMapKeyLighting("NumAdd", negdrkcol, false);
                            GlobalApplyMapKeyLighting("NumEnter", negdrkcol, false);
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", gritcol, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", bloodcol, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", gritcol, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", bloodcol, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", gritcol, false, false);
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

                                GlobalApplyMapKeyLighting("Num4", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num5", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num6", bloodcol, false);

                                GlobalApplyMapKeyLighting("Num1", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num2", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num3", bloodcol, false);

                                GlobalApplyMapKeyLighting("Num0", bloodcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", bloodcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", bloodcol, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", gritcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", gritcol, false, false);
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

                                GlobalApplyMapKeyLighting("Num1", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num2", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num3", bloodcol, false);

                                GlobalApplyMapKeyLighting("Num0", bloodcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", bloodcol, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", bloodcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", bloodcol, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", negdrkcol, false, false);
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", negdrkcol, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", negdrkcol, false, false);
                                }
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
                            switch (Cooldowns.CurrentCard)
                            {
                                case Cooldowns.CardTypes.Arrow:
                                    burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTArrow);
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

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", burstastcol, false, false);
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
                                GlobalApplyMapLightbarLighting("Lightbar19", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", burstastcol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", burstastcol, false, false);
                            }
                        }
                        break;
                    case Actor.Job.MCH:
                        //Machinist
                        var ammo = Cooldowns.AmmoCount;

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

                        if (Cooldowns.GaussBarrelEnabled)
                        {
                            var mchoverheat = Cooldowns.OverHeatTime;
                            var mchgb = Cooldowns.HeatGauge;

                            if (mchoverheat > 0)
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

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", heatover, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", heatover, false, false);
                                }
                            }
                            else
                            {
                                //Normal
                                var polGB = (mchgb - 0) * (50 - 0) / (100 - 0) + 0;
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", heatnormal, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", heatnormal, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", heatnormal, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", heatnormal, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", heatnormal, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", heatnormal, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", negmchburst, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", negmchburst, false, false);
                                    }
                                }
                            }
                        }
                        else
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

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", negmchburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", negmchburst, false, false);
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

                        //Console.WriteLine(@"RDM: " + Cooldowns.BlackMana.ToString());

                        var blackburst = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobRDMBlackMana);
                        var whiteburst = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobRDMWhiteMana);
                        var negburst = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobRDMNegative);

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

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", blackburst, false, false);
                            }
                        }
                        else if (polBlack <= 30 && polBlack > 20)
                        {
                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                            GlobalApplyMapKeyLighting("Num9", blackburst, false);
                            GlobalApplyMapKeyLighting("Num6", blackburst, false);
                            GlobalApplyMapKeyLighting("Num3", blackburst, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", blackburst, false, false);
                            }
                        }
                        else if (polBlack <= 20 && polBlack > 10)
                        {
                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                            GlobalApplyMapKeyLighting("Num9", negburst, false);
                            GlobalApplyMapKeyLighting("Num6", blackburst, false);
                            GlobalApplyMapKeyLighting("Num3", blackburst, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", blackburst, false, false);
                            }
                        }
                        else if (polBlack <= 10 && polBlack > 0)
                        {
                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                            GlobalApplyMapKeyLighting("Num9", negburst, false);
                            GlobalApplyMapKeyLighting("Num6", negburst, false);
                            GlobalApplyMapKeyLighting("Num3", blackburst, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", blackburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", blackburst, false, false);
                            }
                        }
                        else if (polBlack == 0)
                        {
                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                            GlobalApplyMapKeyLighting("Num9", negburst, false);
                            GlobalApplyMapKeyLighting("Num6", negburst, false);
                            GlobalApplyMapKeyLighting("Num3", negburst, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", negburst, false, false);
                            }
                        }


                        //White
                        if (polWhite <= 40 && polWhite > 30)
                        {
                            GlobalApplyMapKeyLighting("NumLock", whiteburst, false);
                            GlobalApplyMapKeyLighting("Num7", whiteburst, false);
                            GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar9", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", whiteburst, false, false);
                            }
                        }
                        else if (polWhite <= 30 && polWhite > 20)
                        {
                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                            GlobalApplyMapKeyLighting("Num7", whiteburst, false);
                            GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar9", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", negburst, false, false);
                            }
                        }
                        else if (polWhite <= 20 && polWhite > 10)
                        {
                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                            GlobalApplyMapKeyLighting("Num7", negburst, false);
                            GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar9", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", negburst, false, false);
                            }
                        }
                        else if (polWhite <= 10 && polWhite > 0)
                        {
                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                            GlobalApplyMapKeyLighting("Num7", negburst, false);
                            GlobalApplyMapKeyLighting("Num4", negburst, false);
                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar9", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", whiteburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", negburst, false, false);
                            }
                        }
                        else if (polWhite == 0)
                        {
                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                            GlobalApplyMapKeyLighting("Num7", negburst, false);
                            GlobalApplyMapKeyLighting("Num4", negburst, false);
                            GlobalApplyMapKeyLighting("Num1", negburst, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar9", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", negburst, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", negburst, false, false);
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                    }
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

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", negcraftercol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", negcraftercol, false, false);
                                    }

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
                                GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", negcraftercol, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", negcraftercol, false, false);
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
                            GlobalApplyMapLightbarLighting("Lightbar19", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar18", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar17", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar16", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar15", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar14", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar13", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar12", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar11", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar10", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar9", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar8", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar7", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar6", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar5", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar4", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar3", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar2", baseColor, false, false);
                            GlobalApplyMapLightbarLighting("Lightbar1", baseColor, false, false);
                        }

                        break;

                }
            }
            else
            {
                ToggleGlobalFlash3(false);
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
                    GlobalApplyMapLightbarLighting("Lightbar19", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar18", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar17", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar16", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar15", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar14", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar13", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar12", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar11", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar10", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar9", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar8", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar7", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar6", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar5", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar4", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar3", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar2", baseColor, false, false);
                    GlobalApplyMapLightbarLighting("Lightbar1", baseColor, false, false);
                }
            }
        }
    }
}
