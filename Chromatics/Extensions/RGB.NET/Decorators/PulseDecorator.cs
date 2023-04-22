using RGB.NET.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class PulseDecorator : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly RGBDeviceType deviceType;
        private readonly int interval;
        private readonly int stepspeed;
        private readonly bool oneshot;
        private readonly double fadeSpeed;
        private readonly Color highlightColor;
        private readonly Color baseColor;
        private ConcurrentDictionary<Led, Color> fadingInLeds;
        private ConcurrentDictionary<Led, Color> fadingOutLeds;
        private Dictionary<Led, float> currentBrightness;
        private Dictionary<Led, Color> currentColors;
        private Dictionary<Led, Color> savedColors;
        private double Timing;
        private double stepDelay;
        private double updateCounter = 0;
        private int globalStep;

        public PulseDecorator(ListLedGroup _ledGroup, int interval, int stepspeed, double fadeSpeed, Color highlightColor, RGBSurface surface, RGBDeviceType deviceType, bool updateIfDisabled = false, Color baseColor = default(Color), bool oneshot = false) : base(surface, updateIfDisabled)
        {
            this.ledGroup = _ledGroup;
            this.deviceType = deviceType;
            this.interval = interval;
            this.stepspeed = stepspeed;
            this.fadeSpeed = fadeSpeed;
            this.highlightColor = highlightColor;
            this.oneshot = oneshot;
            this.baseColor = baseColor == default(Color) ? Color.Transparent : baseColor;

            fadingInLeds = new ConcurrentDictionary<Led, Color>();
            fadingOutLeds = new ConcurrentDictionary<Led, Color>();
            currentBrightness = new Dictionary<Led, float>();
            currentColors = new Dictionary<Led, Color>();
            savedColors = new Dictionary<Led, Color>();
            Timing = 0;
            stepDelay = 0;
            globalStep = 0;
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);

            ledGroup.Detach();
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);

            currentBrightness.Clear();
            fadingInLeds.Clear();
            fadingOutLeds.Clear();
            currentColors.Clear();
            savedColors.Clear();
            Timing = 0;
            stepDelay = 0;

            currentBrightness = null;
            fadingInLeds = null;
            fadingOutLeds = null;
            currentColors = null;
            savedColors = null;
        }

        protected override void Update(double deltaTime)
        {
            // increment the Timing variable
            Timing += deltaTime;

            var minBrightness = 0;
            var maxBrightness = 1;

            if (Timing >= stepDelay)
            {

                if (globalStep == 0)
                {
                    //Setup Pulse
                    foreach (var led in Surface.Leds)
                    {
                        savedColors.Add(led, led.Color);
                    }

                    if (deviceType == RGBDeviceType.Keyboard)
                    {
                        foreach (var led in ledGroup)
                        {
                            if (pulseSteps[1].Contains(led.Id))
                            {
                                fadingInLeds.TryAdd(led, savedColors[led]);
                                currentColors.Add(led, savedColors[led]);
                            }
                            else
                            {
                                currentColors.Add(led, savedColors[led]);
                            }
                        }

                    }
                    else
                    {
                        //var availableLeds = ledGroup.ToList().Except(fadingInLeds.Keys).Except(fadingOutLeds.Keys);
                        var availableLeds = ledGroup.Where(led => !fadingInLeds.ContainsKey(led) && !fadingOutLeds.ContainsKey(led));
                        foreach (var led in availableLeds)
                        {
                            fadingInLeds.TryAdd(led, savedColors[led]);
                            currentColors.Add(led, savedColors[led]);
                        }
                    }

                    globalStep = 1;
                }
                else if (globalStep == 1)
                {
                    //Start Fade//Pulse Step 1
                    foreach (var led in fadingInLeds)
                    {
                        // check if the current brightness of the LED is already stored in the dictionary
                        if (!currentBrightness.ContainsKey(led.Key))
                        {
                            // if not, add the current brightness of the LED to the dictionary
                            currentBrightness.Add(led.Key, minBrightness);
                        }

                        // increment the current brightness of the LED
                        currentBrightness[led.Key] += (float)(deltaTime * (maxBrightness - minBrightness) / (fadeSpeed / 1000));

                        // check if the current brightness of the LED has reached the max brightness
                        if (currentBrightness[led.Key] >= maxBrightness)
                        {
                            // if it has, add the LED to the list of fading out LEDs if not keyboard
                            if (deviceType != RGBDeviceType.Keyboard)
                                fadingOutLeds.TryAdd(led.Key, highlightColor);

                            // remove the LED from the list of fading in LEDs
                            fadingInLeds.TryRemove(led);
                        }
                        else
                        {
                            // if not, choose a random color from the passed in color array
                                        
                            var lerpAmount = Map(currentBrightness[led.Key], minBrightness, maxBrightness, 0, 1);
                            var color = Lerp(savedColors[led.Key], highlightColor, lerpAmount);
                            fadingInLeds[led.Key] = color;
                            currentColors[led.Key] = color;
                        }
                    }

                    if (fadingInLeds.Count == 0)
                        globalStep = 2;
                }
                else if (globalStep == 2)
                {
                    //Pulse Step 2
                    if (deviceType == RGBDeviceType.Keyboard)
                    {
                        foreach (var led in ledGroup)
                        {
                            if (pulseSteps[2].Contains(led.Id))
                            {
                                currentColors[led] = highlightColor;
                            }
                            else
                            {
                                currentColors[led] = savedColors[led];
                            }
                        }
                    }

                    globalStep = 3;
                    stepDelay = Timing + (stepspeed / 1000);
                }
                else if (globalStep == 3)
                {
                    //Pulse Step 3
                    if (deviceType == RGBDeviceType.Keyboard)
                    {
                        foreach (var led in ledGroup)
                        {
                            if (pulseSteps[3].Contains(led.Id))
                            {
                                currentColors[led] = highlightColor;
                            }
                            else
                            {
                                currentColors[led] = savedColors[led];
                            }
                        }
                    }

                    globalStep = 4;
                    stepDelay = Timing + (stepspeed / 1000);
                }
                else if (globalStep == 4)
                {
                    //Pulse Step 4
                    if (deviceType == RGBDeviceType.Keyboard)
                    {
                        foreach (var led in ledGroup)
                        {
                            if (pulseSteps[4].Contains(led.Id))
                            {
                                currentColors[led] = highlightColor;
                            }
                            else
                            {
                                currentColors[led] = savedColors[led];
                            }
                        }
                    }

                    globalStep = 5;
                    stepDelay = Timing + (stepspeed / 1000);
                }
                else if (globalStep == 5)
                {
                    //Pulse Step 5
                    if (deviceType == RGBDeviceType.Keyboard)
                    {
                        foreach (var led in ledGroup)
                        {
                            if (pulseSteps[5].Contains(led.Id))
                            {
                                currentColors[led] = highlightColor;
                            }
                            else
                            {
                                currentColors[led] = savedColors[led];
                            }
                        }
                    }

                    globalStep = 6;
                    stepDelay = Timing + (stepspeed / 1000);
                }
                else if (globalStep == 6)
                {
                    //Pulse Step 6
                    if (deviceType == RGBDeviceType.Keyboard)
                    {
                        foreach (var led in ledGroup)
                        {
                            if (pulseSteps[6].Contains(led.Id))
                            {
                                currentColors[led] = highlightColor;
                            }
                            else
                            {
                                currentColors[led] = savedColors[led];
                            }
                        }
                    }

                    globalStep = 7;
                    stepDelay = Timing + (stepspeed / 1000);
                }
                else if (globalStep == 7)
                {
                    //Pulse Step 7
                    if (deviceType == RGBDeviceType.Keyboard)
                    {
                        foreach (var led in ledGroup)
                        {
                            if (pulseSteps[7].Contains(led.Id))
                            {
                                currentColors[led] = highlightColor;
                            }
                            else
                            {
                                currentColors[led] = savedColors[led];
                            }
                        }
                    }

                    globalStep = 8;
                    stepDelay = Timing + (stepspeed / 1000);
                }
                else if (globalStep == 8)
                {
                    //Pulse Step 8
                    if (deviceType == RGBDeviceType.Keyboard)
                    {
                        foreach (var led in ledGroup)
                        {
                            if (pulseSteps[8].Contains(led.Id))
                            {
                                currentColors[led] = highlightColor;
                                fadingOutLeds.TryAdd(led, highlightColor);
                            }
                            else
                            {
                                currentColors[led] = savedColors[led];
                            }
                        }
                    }

                    globalStep = 9;
                    stepDelay = Timing + (stepspeed / 1000);
                }
                else if (globalStep == 9)
                {
                    //Setup Reset
                    foreach (var led in fadingOutLeds)
                    {
                        // check if the current brightness of the LED is already stored in the dictionary
                        if (!currentBrightness.ContainsKey(led.Key))
                        {
                            // if not, add the current brightness of the LED to the dictionary
                            currentBrightness.Add(led.Key, maxBrightness);
                        }

                        // decrement the current brightness of the LED
                        currentBrightness[led.Key] -= (float)(deltaTime * (maxBrightness - minBrightness) / (fadeSpeed / 1000));

                        // check if the current brightness of the LED has reached the min brightness
                        if (currentBrightness[led.Key] <= minBrightness)
                        {
                            // if it has, remove the LED from the list of fading out LEDs
                            fadingOutLeds.TryRemove(led);
                            currentBrightness.Remove(led.Key);
                        }
                        else
                        {
                            // if not, set the LED's color to the base color with the current brightness
                            var lerpAmount = Map(currentBrightness[led.Key], minBrightness, maxBrightness, 0, 1);
                            var color = Lerp(savedColors[led.Key], highlightColor, lerpAmount);
                            fadingOutLeds[led.Key] = color;
                            currentColors[led.Key] = color;

                        }
                    }

                    //Reset
                    if (fadingOutLeds.Count == 0)
                    {
                        if (oneshot)
                        {
                            Detach();
                            return;
                        }
                        else
                        {
                            foreach (var led in ledGroup)
                            {
                                led.Color = savedColors[led];
                            } 

                            currentBrightness.Clear();
                            fadingInLeds.Clear();
                            fadingOutLeds.Clear();
                            currentColors.Clear();
                            savedColors.Clear();
                            Timing = 0;
                            globalStep = 0;
                            stepDelay = Timing + (interval / 1000);
                        }

                    }
                }
            }

            foreach (var led in currentColors)
            {
                led.Key.Color = led.Value;
                //Surface.Update(true);
            }
            
        }


        private float Map(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
        }

        private static Color Lerp(Color start, Color end, float amount)
        {
            float r = start.R + (end.R - start.R) * amount;
            float g = start.G + (end.G - start.G) * amount;
            float b = start.B + (end.B - start.B) * amount;

            return new Color((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        private void WaitForInterval(double deltaTime)
        {
            updateCounter += deltaTime;

            if (updateCounter >= (interval / 1000))
            {
                updateCounter = 0;
                
                
            }
        }

        private bool ShouldUpdate(double deltaTime)
        {
            updateCounter += deltaTime;
            if (updateCounter >= interval / 1000)
            {
                return true;
            }
            return false;
        }

        private void ResetUpdateCounter()
        {
            updateCounter = 0;
        }

        public static readonly Dictionary<int, LedId[]> pulseSteps= new Dictionary<int, LedId[]>()
        {
            {1, new LedId[] {LedId.Keyboard_Y} },
            {2, new LedId[] {LedId.Keyboard_5, LedId.Keyboard_6, LedId.Keyboard_7, LedId.Keyboard_T, LedId.Keyboard_U, LedId.Keyboard_G, LedId.Keyboard_H, LedId.Keyboard_J}},
            {3, new LedId[] {LedId.Keyboard_F3, LedId.Keyboard_F4, LedId.Keyboard_F5, LedId.Keyboard_F6, LedId.Keyboard_F7, LedId.Keyboard_4, LedId.Keyboard_8, LedId.Keyboard_R, LedId.Keyboard_F, LedId.Keyboard_C, LedId.Keyboard_V, LedId.Keyboard_B, LedId.Keyboard_N, LedId.Keyboard_M, LedId.Keyboard_K, LedId.Keyboard_I} },
            {4, new LedId[] {LedId.Keyboard_F2, LedId.Keyboard_F8, LedId.Keyboard_3, LedId.Keyboard_E, LedId.Keyboard_D, LedId.Keyboard_X, LedId.Keyboard_Space, LedId.Keyboard_CommaAndLessThan, LedId.Keyboard_L, LedId.Keyboard_O, LedId.Keyboard_9} },
            {5, new LedId[] {LedId.Keyboard_F1, LedId.Keyboard_F9, LedId.Keyboard_2, LedId.Keyboard_0, LedId.Keyboard_W, LedId.Keyboard_S, LedId.Keyboard_Z, LedId.Keyboard_LeftAlt, LedId.Keyboard_P, LedId.Keyboard_SemicolonAndColon, LedId.Keyboard_PeriodAndBiggerThan, LedId.Keyboard_RightAlt} },
            {6, new LedId[] {LedId.Keyboard_1, LedId.Keyboard_Q, LedId.Keyboard_A, LedId.Keyboard_WinLock, LedId.Keyboard_F10, LedId.Keyboard_MinusAndUnderscore, LedId.Keyboard_BracketLeft, LedId.Keyboard_ApostropheAndDoubleQuote, LedId.Keyboard_SlashAndQuestionMark, LedId.Keyboard_Function} },
            {7, new LedId[] {LedId.Keyboard_Escape, LedId.Keyboard_Escape, LedId.Keyboard_Tab, LedId.Keyboard_CapsLock, LedId.Keyboard_LeftShift, LedId.Keyboard_LeftCtrl, LedId.Keyboard_F11, LedId.Keyboard_EqualsAndPlus, LedId.Keyboard_RightGui, LedId.Keyboard_BracketRight, LedId.Keyboard_Backslash} },
            {8, new LedId[] {LedId.Keyboard_F12, LedId.Keyboard_Backspace, LedId.Keyboard_Enter, LedId.Keyboard_RightShift, LedId.Keyboard_RightCtrl} },
        };

    }
}
