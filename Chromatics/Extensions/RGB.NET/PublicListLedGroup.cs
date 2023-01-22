using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET
{
    public class PublicListLedGroup : ListLedGroup
    {
        #pragma warning disable CS8632

        public IList<Led> PublicGroupLeds => GroupLeds;
        public PublicListLedGroup(RGBSurface? surface) : base(surface) { }
        public PublicListLedGroup(RGBSurface? surface, IEnumerable<Led> leds) : base(surface, leds) { }
        public PublicListLedGroup(RGBSurface? surface, params Led[] leds) : base(surface, leds) { }

        #pragma warning restore CS8632

    }
}
