using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chromatics.Helpers
{
    public static class SizeHelper
    {
        public static Size ResizeKeepAspect(this Size src, int maxWidth, int maxHeight, bool enlarge = false)
        {
            maxWidth = enlarge ? maxWidth : Math.Min(maxWidth, src.Width);
            maxHeight = enlarge ? maxHeight : Math.Min(maxHeight, src.Height);

            decimal rnd = Math.Min(maxWidth / (decimal)src.Width, maxHeight / (decimal)src.Height);
            return new Size((int)Math.Round(src.Width * rnd), (int)Math.Round(src.Height * rnd));
        }

        public static Padding GetCorrectionPadding(TableLayoutPanel TLP, int minimumPadding)
        {
            int minPad = minimumPadding;
            Rectangle netRect = TLP.ClientRectangle;
            netRect.Inflate(-minPad, -minPad);

            int w = netRect.Width / TLP.ColumnCount;
            int h = netRect.Height / TLP.RowCount;

            int deltaX = (netRect.Width - w * TLP.ColumnCount) / 2;
            int deltaY = (netRect.Height - h * TLP.RowCount) / 2;

            int OddX = (netRect.Width - w * TLP.ColumnCount) % 2;
            int OddY = (netRect.Height - h * TLP.RowCount) % 2;

            return new Padding(minPad + deltaX, minPad + deltaY, minPad + deltaX + OddX, minPad + deltaY + OddY);
        }
    }
}
