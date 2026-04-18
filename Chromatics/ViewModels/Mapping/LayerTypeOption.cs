using Chromatics.Enums;

namespace Chromatics.ViewModels.Mapping
{
    // Entry in the layer's inner type combobox (e.g. Static / Reactive Weather
    // for a BaseLayer, or Highlight / Keybinds for a DynamicLayer).
    public sealed class LayerTypeOption
    {
        public string Name { get; }
        public int Value { get; }
        public LayerModes[] SupportedModes { get; }

        public LayerTypeOption(string name, int value, LayerModes[] supportedModes)
        {
            Name = name;
            Value = value;
            SupportedModes = supportedModes;
        }

        public override string ToString() => Name;
    }
}
