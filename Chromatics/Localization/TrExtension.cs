#nullable enable
using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Chromatics.Localization
{
    /// <summary>
    /// Markup extension for live-updating localization bindings.
    /// Usage: Text="{loc:Tr Key=Console}"
    ///        Text="{loc:Tr Key='Remind Me Later'}"
    ///
    /// Binds to LocalizationService.Instance.Version (a simple int property —
    /// the binding path parser can't handle indexer keys with spaces, so we
    /// trigger off a plain property and use a converter to pull the translation
    /// from the service's indexer every time the version bumps).
    /// </summary>
    public sealed class TrExtension : MarkupExtension
    {
        [ConstructorArgument("key")]
        public string Key { get; set; } = string.Empty;

        public TrExtension() { }
        public TrExtension(string key) { Key = key; }

        public override object ProvideValue(IServiceProvider serviceProvider)
            => new Binding(nameof(LocalizationService.Version))
            {
                Source = LocalizationService.Instance,
                Mode = BindingMode.OneWay,
                Converter = new KeyConverter(Key),
            };

        private sealed class KeyConverter : IValueConverter
        {
            private readonly string _key;
            public KeyConverter(string key) => _key = key;

            public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
                => LocalizationService.Instance[_key];

            public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
                => throw new NotSupportedException();
        }
    }
}
