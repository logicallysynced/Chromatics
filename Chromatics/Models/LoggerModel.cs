using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Models
{
    public class OnConsoleLoggedEventArgs : EventArgs
    {
        private string _message;
        private Color _color;

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public Color Color
        {
            get { return _color; }
            set { _color = value; }
        }

        public OnConsoleLoggedEventArgs(string message, Color color)
        {
            Message = message;
            Color = color;
        }
    }
}
