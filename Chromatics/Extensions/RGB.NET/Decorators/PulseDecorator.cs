using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class PulseDecorator : AbstractUpdateAwareDecorator, IBrushDecorator
    {
        public Color Color { get; set; } = new Color(0, 0, 0);

        private Color _color;

        private Dictionary<LedId, Color> _colors = new Dictionary<LedId, Color>();

        private Dictionary<LedId, Color> _toSet = new Dictionary<LedId, Color>();

        private Dictionary<LedId, Led> _list = new Dictionary<LedId, Led>();

        public PulseDecorator(RGBSurface surface, bool updateIfDisabled = false) : base(surface, updateIfDisabled)
        {
            _color = Color;

            foreach (var key in surface.Leds)
            {
                if (!_colors.ContainsKey(key.Id))
                {
                    var prev = key.Color;
                    _colors.Add(key.Id, prev);
                    _toSet.Add(key.Id, prev);
                    _list.Add(key.Id, key);
                }
            }
        }

        public void ManipulateColor(in Rectangle rectangle, in RenderTarget renderTarget, ref Color color)
        {
            if (_toSet.ContainsKey(renderTarget.Led.Id))
            {
                color = _toSet[renderTarget.Led.Id];
                Debug.WriteLine("set: " + color);
            }
        }

        protected override void Update(double deltaTime)
        {
            //
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);

            for (var i = 0; i <= 9; i++)
            {
                Debug.WriteLine(i);
                Update(1000);

                if (i == 1)
                {
                    //Step 0
                    foreach (var key in PulseSteps.GlobalKeys)
                    {
                        var pos = Array.IndexOf(PulseSteps.PulseOutStep0, key);
                        if (pos > -1)
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _color;
                                _list[key].Color = _color;
                            }
                        }
                        else
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _colors[key];
                                _list[key].Color = _colors[key];
                            }
                        }
                                                
                    }
                }
                else if (i == 2)
                {
                    //Step 1
                    foreach (var key in PulseSteps.GlobalKeys)
                    {
                        var pos = Array.IndexOf(PulseSteps.PulseOutStep1, key);
                        if (pos > -1)
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _color;
                                _list[key].Color = _color;
                            }
                        }
                        else
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _colors[key];
                                _list[key].Color = _colors[key];
                            }
                        }
                    }
                }
                else if (i == 3)
                {
                    //Step 2
                    foreach (var key in PulseSteps.GlobalKeys)
                    {
                        var pos = Array.IndexOf(PulseSteps.PulseOutStep2, key);
                        if (pos > -1)
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _color;
                                _list[key].Color = _color;
                            }
                        }
                        else
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _colors[key];
                                _list[key].Color = _colors[key];
                            }
                        }
                    }
                }
                else if (i == 4)
                {
                    //Step 3
                    foreach (var key in PulseSteps.GlobalKeys)
                    {
                        var pos = Array.IndexOf(PulseSteps.PulseOutStep3, key);
                        if (pos > -1)
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _color;
                                _list[key].Color = _color;
                            }
                        }
                        else
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _colors[key];
                                _list[key].Color = _colors[key];
                            }
                        }
                    }
                }
                else if (i == 5)
                {
                    //Step 4
                    foreach (var key in PulseSteps.GlobalKeys)
                    {
                        var pos = Array.IndexOf(PulseSteps.PulseOutStep4, key);
                        if (pos > -1)
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _color;
                                _list[key].Color = _color;
                            }
                        }
                        else
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _colors[key];
                                _list[key].Color = _colors[key];
                            }
                        }
                    }
                }
                else if (i == 6)
                {
                    //Step 5
                    foreach (var key in PulseSteps.GlobalKeys)
                    {
                        var pos = Array.IndexOf(PulseSteps.PulseOutStep5, key);
                        if (pos > -1)
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _color;
                                _list[key].Color = _color;
                            }
                        }
                        else
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _colors[key];
                                _list[key].Color = _colors[key];
                            }
                        }
                    }
                }
                else if (i == 7)
                {
                    //Step 6
                    foreach (var key in PulseSteps.GlobalKeys)
                    {
                        var pos = Array.IndexOf(PulseSteps.PulseOutStep6, key);
                        if (pos > -1)
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _color;
                                _list[key].Color = _color;
                            }
                        }
                        else
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _colors[key];
                                _list[key].Color = _colors[key];
                            }
                        }
                    }
                }
                else if (i == 8)
                {
                    //Step 7
                    foreach (var key in PulseSteps.GlobalKeys)
                    {
                        var pos = Array.IndexOf(PulseSteps.PulseOutStep7, key);
                        if (pos > -1)
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _color;
                                _list[key].Color = _color;
                            }
                        }
                        else
                        {
                            if (_colors.ContainsKey(key))
                            {
                                _toSet[key] = _colors[key];
                                _list[key].Color = _colors[key];
                            }
                        }
                    }
                }
                else if (i == 9)
                {
                    //Spin down
                    Detach();

                }
            }
        }
            
    }

    internal class PulseSteps
    {
        //Pulse Out From Centre
        public static readonly LedId[] GlobalKeys = { LedId.Keyboard_Y, LedId.Keyboard_5, LedId.Keyboard_6, LedId.Keyboard_7, LedId.Keyboard_T, LedId.Keyboard_U, LedId.Keyboard_G, LedId.Keyboard_H, LedId.Keyboard_J, LedId.Keyboard_F3, LedId.Keyboard_F4, LedId.Keyboard_F5, 
                LedId.Keyboard_F6, LedId.Keyboard_F7, LedId.Keyboard_4, LedId.Keyboard_8, LedId.Keyboard_R, LedId.Keyboard_F, LedId.Keyboard_C, LedId.Keyboard_V, LedId.Keyboard_B, LedId.Keyboard_N, LedId.Keyboard_M, LedId.Keyboard_K, LedId.Keyboard_I, LedId.Keyboard_F2, 
                LedId.Keyboard_F8, LedId.Keyboard_3, LedId.Keyboard_E, LedId.Keyboard_D, LedId.Keyboard_X, LedId.Keyboard_Space, LedId.Keyboard_CommaAndLessThan, LedId.Keyboard_L, LedId.Keyboard_O, LedId.Keyboard_9, LedId.Keyboard_F1, LedId.Keyboard_F9, LedId.Keyboard_2, 
                LedId.Keyboard_0, LedId.Keyboard_W, LedId.Keyboard_S, LedId.Keyboard_Z, LedId.Keyboard_LeftAlt, LedId.Keyboard_P, LedId.Keyboard_SemicolonAndColon, LedId.Keyboard_PeriodAndBiggerThan, LedId.Keyboard_RightAlt, LedId.Keyboard_1, LedId.Keyboard_Q, LedId.Keyboard_A, 
                LedId.Keyboard_LeftGui, LedId.Keyboard_F10, LedId.Keyboard_MinusAndUnderscore, LedId.Keyboard_BracketLeft, LedId.Keyboard_ApostropheAndDoubleQuote, LedId.Keyboard_SlashAndQuestionMark, LedId.Keyboard_RightGui, LedId.Keyboard_Escape, LedId.Keyboard_GraveAccentAndTilde, 
                LedId.Keyboard_Tab, LedId.Keyboard_CapsLock, LedId.Keyboard_LeftShift, LedId.Keyboard_LeftCtrl, LedId.Keyboard_F11, LedId.Keyboard_EqualsAndPlus, LedId.Keyboard_Application, LedId.Keyboard_BracketRight, LedId.Keyboard_Backslash, LedId.Keyboard_F12, LedId.Keyboard_Backspace, 
                LedId.Keyboard_Enter, LedId.Keyboard_RightShift, LedId.Keyboard_RightCtrl };

        public static readonly LedId[] PulseOutStep0 = { LedId.Keyboard_Y };

        public static readonly LedId[] PulseOutStep1 = { LedId.Keyboard_5, LedId.Keyboard_6, LedId.Keyboard_7, LedId.Keyboard_T, LedId.Keyboard_U, LedId.Keyboard_G, LedId.Keyboard_H, LedId.Keyboard_J };

        public static readonly LedId[] PulseOutStep2 =
            {LedId.Keyboard_F3, LedId.Keyboard_F4, LedId.Keyboard_F5, LedId.Keyboard_F6, LedId.Keyboard_F7, LedId.Keyboard_4, LedId.Keyboard_8, LedId.Keyboard_R, LedId.Keyboard_F, LedId.Keyboard_C, LedId.Keyboard_V, LedId.Keyboard_B, LedId.Keyboard_N, LedId.Keyboard_M, LedId.Keyboard_K, LedId.Keyboard_I};

        public static readonly LedId[] PulseOutStep3 =
            {LedId.Keyboard_F2, LedId.Keyboard_F8, LedId.Keyboard_3, LedId.Keyboard_E, LedId.Keyboard_D, LedId.Keyboard_X, LedId.Keyboard_Space, LedId.Keyboard_CommaAndLessThan, LedId.Keyboard_L, LedId.Keyboard_O, LedId.Keyboard_9};

        public static readonly LedId[] PulseOutStep4 =
            {LedId.Keyboard_F1, LedId.Keyboard_F9, LedId.Keyboard_2, LedId.Keyboard_0, LedId.Keyboard_W, LedId.Keyboard_S, LedId.Keyboard_Z, LedId.Keyboard_LeftAlt, LedId.Keyboard_P, LedId.Keyboard_SemicolonAndColon, LedId.Keyboard_PeriodAndBiggerThan, LedId.Keyboard_RightAlt};

        public static readonly LedId[] PulseOutStep5 =
        {
            LedId.Keyboard_1, LedId.Keyboard_Q, LedId.Keyboard_A, LedId.Keyboard_LeftGui, LedId.Keyboard_F10, LedId.Keyboard_MinusAndUnderscore, LedId.Keyboard_BracketLeft, LedId.Keyboard_ApostropheAndDoubleQuote, LedId.Keyboard_SlashAndQuestionMark, LedId.Keyboard_RightGui
        };

        public static readonly LedId[] PulseOutStep6 =
        {
            LedId.Keyboard_Escape, LedId.Keyboard_GraveAccentAndTilde, LedId.Keyboard_Tab, LedId.Keyboard_CapsLock, LedId.Keyboard_LeftShift, LedId.Keyboard_LeftCtrl, LedId.Keyboard_F11, LedId.Keyboard_EqualsAndPlus, LedId.Keyboard_Application,
            LedId.Keyboard_BracketRight, LedId.Keyboard_Backslash
        };

        public static readonly LedId[] PulseOutStep7 =
        {
            LedId.Keyboard_F12, LedId.Keyboard_Backspace, LedId.Keyboard_Enter, LedId.Keyboard_RightShift, LedId.Keyboard_RightCtrl
        };
    }
}
