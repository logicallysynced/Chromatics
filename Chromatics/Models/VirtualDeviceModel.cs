using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Layers;
using OpenRGB.NET;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;

namespace Chromatics.Models
{

    public class VirtualDevice : UserControl
    {
        public int _width = 35;
        public int _height = 35;
        public bool init;
        public RGBDeviceType _deviceType;
        public Guid _deviceId;
        public List<KeyButton> _keybuttons = new List<KeyButton>();
        public event EventHandler _OnKeycapPressed;
        private Dictionary<LedId, Color> _currentColors;
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public virtual void InitializeDevice()
        {
            // To be overridden by derived classes
        }

        public void OnKeycapPressed(object sender, EventArgs e)
        {
            if (_OnKeycapPressed != null)
                _OnKeycapPressed(sender, e);
        }

        public VirtualDevice()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        public async void VisualiseLayers(IOrderedEnumerable<KeyValuePair<int, Layer>> _layers, List<KeyButton> _keyButtons)
        {
            if (_currentColors == null)
            {
                _currentColors = new Dictionary<LedId, Color>();
            }
            
            if (MappingLayers.IsPreview())
            {
                // Debug.WriteLine("Tick");
                var activeSurface = RGBController.GetLiveSurfaces();
                var devicesOfType = activeSurface.Devices.Where(device => device.DeviceInfo.DeviceType == _deviceType);

                _cancellationTokenSource = new CancellationTokenSource(); // Create a new token source
                var token = _cancellationTokenSource.Token;

                var tasks = devicesOfType.Select(device => Task.Run(() =>
                {
                    foreach (var led in device)
                    {
                        if (token.IsCancellationRequested)
                        {
                            Debug.WriteLine("Task canceled.");
                            break;
                        }

                        var activeCol = System.Drawing.Color.FromArgb(
                            (int)(led.Color.A * 255),
                            (int)(led.Color.R * 255),
                            (int)(led.Color.G * 255),
                            (int)(led.Color.B * 255)
                        );

                        foreach (var key in _keybuttons.Where(keys => keys.KeyType == led.Id))
                        {
                            if (!key.IsEditing)
                            {
                                lock (_currentColors)
                                {
                                    if (_currentColors.ContainsKey(key.KeyType))
                                    {
                                        if (_currentColors[key.KeyType] != activeCol)
                                        {
                                            _currentColors[key.KeyType] = activeCol;
                                            key.BackColor = activeCol;
                                        }
                                    }
                                    else
                                    {
                                        _currentColors.Add(key.KeyType, activeCol);
                                        key.BackColor = activeCol;
                                    }
                                }
                            }
                        }

                    }
                }, token)).ToArray();

                await Task.WhenAll(tasks);

                return;
            }
            else
            {
                // Cancel the tasks if MappingLayers.IsPreview() becomes false
                if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
            
            // Code to visualize the layers on the virtual device control
            await Task.Run(() =>
            {
                foreach (var layer in _layers)
                {
                    var mapping = layer.Value;
                    var blank_col = Color.DarkGray;

                    if (layer.Value.deviceType != _deviceType) return;

                    if (mapping.rootLayerType == LayerType.BaseLayer && !mapping.Enabled)
                    {
                        foreach (var key in _keybuttons)
                        {
                            if (mapping.deviceLeds.ContainsValue(key.KeyType))
                            {
                                lock (_currentColors)
                                {
                                    if (_currentColors.ContainsKey(key.KeyType))
                                    {
                                        if (_currentColors[key.KeyType] != blank_col)
                                        {
                                            _currentColors[key.KeyType] = blank_col;
                                            key.BackColor = blank_col;
                                        }
                                    }
                                    else
                                    {
                                        _currentColors.Add(key.KeyType, blank_col);
                                        key.BackColor = blank_col;
                                    }
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"KeyType {key.KeyType} not found in deviceLeds");
                            }
                        }

                        continue;
                    }

                    if (!mapping.Enabled || mapping.rootLayerType == LayerType.EffectLayer) continue;

                    var highlight_col = (Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(mapping.rootLayerType).Value;

                    foreach (var key in _keybuttons)
                    {
                        if (mapping.deviceLeds.ContainsValue(key.KeyType))
                        {
                            if (!key.IsEditing)
                            {
                                lock (_currentColors)
                                {
                                    if (_currentColors.ContainsKey(key.KeyType))
                                    {
                                        if (_currentColors[key.KeyType] != highlight_col)
                                        {
                                            _currentColors[key.KeyType] = highlight_col;
                                            key.BackColor = highlight_col;
                                        }
                                    }
                                    else
                                    {
                                        _currentColors.Add(key.KeyType, highlight_col);
                                        key.BackColor = highlight_col;
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }



        public class KeyButton : Button
        {
            private bool _drawCircle;
            private string _drawIndex;
            private Color _borderCol;
            private Color _backgroundColor;

            public string KeyName { get; set; }
            public LedId KeyType { get; set; }
            public bool IsEditing { get; set; }

            public Color BorderCol
            {
                get { return _borderCol; }
                set
                {
                    _borderCol = value;
                    this.Invalidate();
                }
            }

            public new Color BackColor
            {
                get { return _backgroundColor; }
                set
                {
                    _backgroundColor = value;
                    this.Invalidate();
                }
            }

            public KeyButton()
            {
                BorderCol = Color.Black;
                BackColor = Color.DarkGray;
                _backgroundColor = BackColor;
            }

            public void RemoveCircle()
            {
                _drawCircle = false;
                IsEditing = false;
                this.Invalidate();
            }

            public void AddCircle(string text)
            {
                _drawIndex = text;
                _drawCircle = true;
                IsEditing = true;
                this.Invalidate();
            }

            GraphicsPath GetRoundPath(RectangleF Rect, int radius)
            {
                float m = 2.75F;
                float r2 = radius / 2f;
                var GraphPath = new GraphicsPath();

                GraphPath.AddArc(Rect.X + m, Rect.Y + m, radius, radius, 180, 90);
                GraphPath.AddLine(Rect.X + r2 + m, Rect.Y + m, Rect.Width - r2 - m, Rect.Y + m);
                GraphPath.AddArc(Rect.X + Rect.Width - radius - m, Rect.Y + m, radius, radius, 270, 90);
                GraphPath.AddLine(Rect.Width - m, Rect.Y + r2, Rect.Width - m, Rect.Height - r2 - m);
                GraphPath.AddArc(Rect.X + Rect.Width - radius - m,
                               Rect.Y + Rect.Height - radius - m, radius, radius, 0, 90);
                GraphPath.AddLine(Rect.Width - r2 - m, Rect.Height - m, Rect.X + r2 - m, Rect.Height - m);
                GraphPath.AddArc(Rect.X + m, Rect.Y + Rect.Height - radius - m, radius, radius, 90, 90);
                GraphPath.AddLine(Rect.X + m, Rect.Height - r2 - m, Rect.X + m, Rect.Y + r2 + m);

                GraphPath.CloseFigure();
                return GraphPath;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                this.SuspendLayout();
                base.OnPaint(e);

                int borderRadius = 10;
                float borderThickness = 1.75f;

                var Rect = new RectangleF(0, 0, this.Width, this.Height);
                var GraphPath = GetRoundPath(Rect, borderRadius);

                // Fill the background color
                using (var brush = new SolidBrush(_backgroundColor))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }

                this.Region = new Region(GraphPath);
                using (var pen = new Pen(BorderCol, borderThickness))
                {
                    pen.Alignment = PenAlignment.Inset;
                    e.Graphics.DrawPath(pen, GraphPath);
                }

                // Draw the text
                var textBrush = new SolidBrush(ForeColor);
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(Text, Font, textBrush, Rect, sf);

                if (_drawCircle)
                {
                    var fnt = new Font(this.Font.FontFamily, this.Font.Size * 2, FontStyle.Bold);
                    var pt = new System.Drawing.Point(3, 3);
                    e.Graphics.DrawString(_drawIndex, fnt, new SolidBrush(Color.White), pt);
                }
                this.ResumeLayout(true);
            }
        }


    }
}
