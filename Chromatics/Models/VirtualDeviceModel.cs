using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chromatics.Models
{
    public class VirtualDevice : UserControl
    {
        public int _width = 35;
        public int _height = 35;
        public bool init;
        public List<KeyButton> _keybuttons = new List<KeyButton>();
        public event EventHandler _OnKeycapPressed;
        public void OnKeycapPressed(object sender, EventArgs e)
        {
            if (_OnKeycapPressed != null)
                _OnKeycapPressed(sender, e);
        }

        public class KeyButton : Button
        {
            private bool _drawCircle;
            
            private string _drawIndex;
            public string KeyName { get; set; }
            public LedId KeyType { get; set; }
            public System.Drawing.Color BorderCol { get; set; }
            public bool IsEditing { get; set; }

            public KeyButton()
            {
                BorderCol = System.Drawing.Color.Black;
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
                base.OnPaint(e);

                int borderRadius = 10;
                float borderThickness = 1.75f;

                var Rect = new RectangleF(0, 0, this.Width, this.Height);
                var GraphPath = GetRoundPath(Rect, borderRadius);

                this.Region = new Region(GraphPath);
                using (var pen = new Pen(BorderCol, borderThickness))
                {
                    pen.Alignment = PenAlignment.Inset;
                    e.Graphics.DrawPath(pen, GraphPath);
                }

                if (_drawCircle)
                {
                    var fnt = new Font(this.Font.FontFamily, this.Font.Size * 2, FontStyle.Bold);
                    var sf = e.Graphics.MeasureString(_drawIndex, fnt, this.Width);
                    var pt = new System.Drawing.Point(3, 3);
                    //pt.X = (int)((this.Width / 2) - (sf.Width / 2));
                    //pt.Y = (int)((this.Height / 2) - (sf.Height / 2));
                    e.Graphics.DrawString(_drawIndex, fnt, new SolidBrush(System.Drawing.Color.White), pt);

                }
            }
        }
    }
}
