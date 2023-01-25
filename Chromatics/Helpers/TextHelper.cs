using Chromatics.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chromatics.Helpers
{
    public class TextHelper
    {
        public static string ParseLayerHelperText(string input)
        {
            //Variable index
            var variables = new Dictionary<string, string>();
            variables.Add("criticalHpPercentage", $"{AppSettings.GetSettings().criticalHpPercentage}%");


            foreach (Match match in Regex.Matches(input, @"\{(.*?)\}"))
            {
                var variableName = match.Groups[1].Value;

                if (variables.ContainsKey(variableName))
                {
                    var variableValue = variables[variableName];
                    input = input.Replace(match.Value, variableValue);
                }

            }

            return input;
        }
    }
}
