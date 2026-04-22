using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Chromatics.Localization;
using System;
using System.ComponentModel;

namespace Chromatics.ViewModels
{
    public sealed class EffectToggleItem : ViewModelBase
    {
        private readonly Action<bool> _commit;
        private readonly string _labelKey;
        private readonly string _tooltipKey;
        private bool _isEnabled;

        public string Label   => LocalizationService.Instance[_labelKey];
        public string Tooltip => LocalizationService.Instance[_tooltipKey];
        public Bitmap IconBitmap { get; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (SetProperty(ref _isEnabled, value))
                    _commit(value);
            }
        }

        public EffectToggleItem(string labelKey, string tooltipKey, string iconUri, bool initialValue, Action<bool> commit)
        {
            _labelKey  = labelKey;
            _tooltipKey = tooltipKey;
            _commit    = commit;
            _isEnabled = initialValue;

            LocalizationService.Instance.PropertyChanged += OnLocaleVersionChanged;

            if (!string.IsNullOrEmpty(iconUri))
            {
                using var stream = AssetLoader.Open(new Uri(iconUri));
                IconBitmap = new Bitmap(stream);
            }
        }

        private void OnLocaleVersionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocalizationService.Version))
            {
                OnPropertyChanged(nameof(Label));
                OnPropertyChanged(nameof(Tooltip));
            }
        }
    }
}
