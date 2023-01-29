using RGB.NET.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class StrobeDecorator : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly PublicListLedGroup ledGroup;
        private readonly Random random = new Random();
        private readonly int numberOfLeds;
        private readonly int interval;
        private readonly bool randomise;
        private readonly bool oneshot;
        private readonly double fadeSpeed;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private ConcurrentDictionary<Led, Color> fadingInLeds;
        private ConcurrentDictionary<Led, Color> fadingOutLeds;
        private Dictionary<Led, float> currentBrightness;
        private Dictionary<Led, Color> currentColors;
        private Dictionary<Led, double> startTimes;
        private double Timing;
        private double startDelay;
        private double updateCounter = 0;

        public StrobeDecorator(PublicListLedGroup _ledGroup, int interval, double fadeSpeed, bool randomise, Color[] colors, RGBSurface surface, bool updateIfDisabled = false, Color baseColor = default(Color), bool oneshot = false) : base(surface, updateIfDisabled)
        {
            this.ledGroup = _ledGroup;
            this.interval = interval;
            this.fadeSpeed = fadeSpeed;
            this.colors = colors;
            this.randomise = randomise;
            this.oneshot = oneshot;
            this.baseColor = baseColor == default(Color) ? Color.Transparent : baseColor;

            startTimes = new Dictionary<Led, double>();
            fadingInLeds = new ConcurrentDictionary<Led, Color>();
            fadingOutLeds = new ConcurrentDictionary<Led, Color>();
            currentBrightness = new Dictionary<Led, float>();
            currentColors = new Dictionary<Led, Color>();
            Timing = 0;
            startDelay = 0;
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
            startTimes.Clear();
            Timing = 0;
            startDelay = 0;

            currentBrightness = null;
            fadingInLeds = null;
            fadingOutLeds = null;
            currentColors = null;
            startTimes = null;
        }

        protected override void Update(double deltaTime)
        {
            // increment the Timing variable
            Timing += deltaTime;

            var minBrightness = 0;
            var maxBrightness = 1;

            if (Timing >= startDelay)
            {
                var availableLeds = ledGroup.PublicGroupLeds.Except(fadingInLeds.Keys).Except(fadingOutLeds.Keys);

                foreach (var led in availableLeds)
                {
                    
                    fadingInLeds.TryAdd(led, baseColor);

                    var colorIndex = random.Next(colors.Length);

                    if (!currentColors.ContainsKey(led))
                        currentColors.Add(led, colors[colorIndex]);

                        
                }

                // Update startDelay to be a few milliseconds earlier than the current interval
                if (!oneshot)
                {
                    if (randomise)
                    {
                        var rng = GetRandomStartTime();

                        if (GetRandomBoolean(0.35))
                        {
                            startDelay = Timing + (fadeSpeed / 1000);
                            Debug.WriteLine($"Hit: {(fadeSpeed / 1000)}");
                        }
                        else
                        {
                            startDelay = Timing + (rng / 1000);
                            Debug.WriteLine($" Random: {rng / 1000}");
                        }

                    
                    }
                    else
                    {
                        startDelay = Timing + (interval / 1000);
                    }
                }
                
            }

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
                    // if it has, add the LED to the list of fading out LEDs
                    fadingOutLeds.TryAdd(led.Key, currentColors[led.Key]);

                    // remove the LED from the list of fading in LEDs
                    fadingInLeds.TryRemove(led);
                }
                else
                {
                    // if not, choose a random color from the passed in color array
                    
                    
                    var lerpAmount = Map(currentBrightness[led.Key], minBrightness, maxBrightness, 0, 1);
                    var color = Lerp(baseColor, currentColors[led.Key], lerpAmount);
                    fadingInLeds[led.Key] = color;

                }
            }

            // iterate through the list of fading out LEDs
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
                    currentColors.Remove(led.Key);
                    startTimes.Remove(led.Key);

                    if (fadingInLeds.ContainsKey(led.Key))
                        fadingInLeds.TryRemove(led);
                }
                else
                {
                    // if not, set the LED's color to the base color with the current brightness
                    var lerpAmount = Map(currentBrightness[led.Key], minBrightness, maxBrightness, 0, 1);
                    var color = Lerp(baseColor, currentColors[led.Key], lerpAmount);
                    fadingOutLeds[led.Key] = color;

                }
            }


            foreach (var led in ledGroup.PublicGroupLeds)
            {
                if (fadingInLeds.ContainsKey(led))
                {
                    led.Color = fadingInLeds[led];
                }
                else if (fadingOutLeds.ContainsKey(led))
                {
                    led.Color = fadingOutLeds[led];
                } 
                else
                {
                    led.Color = baseColor;
                }
                    
            }

            if (oneshot)
            {
                Detach();
            }
        }

        private double GetRandomStartTime()
        {
            int variation = interval / 4; // 50% variation
            int minInterval = interval - variation;
            int maxInterval = interval + variation;

            return random.Next(minInterval, maxInterval);
        }

        private bool GetRandomBoolean(double chance)
        {
            var value = random.NextDouble();
            return value < chance;
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
    }
}
