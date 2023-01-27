using Chromatics.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Extensions
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LayerDisplay : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public LayerModes[] LayerTypeCompatibility { get; set; }
    }
}
