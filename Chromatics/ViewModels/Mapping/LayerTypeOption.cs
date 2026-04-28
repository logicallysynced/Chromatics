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
        public string Description { get; }

        public LayerTypeOption(string name, int value, LayerModes[] supportedModes, string description = null)
        {
            Name = name;
            Value = value;
            SupportedModes = supportedModes;
            Description = description ?? string.Empty;
        }

        public override string ToString() => Name;
    }
}
