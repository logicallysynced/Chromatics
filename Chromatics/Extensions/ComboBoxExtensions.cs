using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Extensions
{
    public class ComboboxItem
    {
        public object Value { get; set; }
        public string Text { get; set; }

        public override string ToString() { return Text; }
    }
}
