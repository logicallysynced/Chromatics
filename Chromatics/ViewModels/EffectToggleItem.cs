using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace Chromatics.ViewModels
{
    public sealed class EffectToggleItem : ViewModelBase
    {
        private readonly Action<bool> _commit;
        private bool _isEnabled;

        public string  Label      { get; }
        public string  Tooltip    { get; }
        public Bitmap  IconBitmap { get; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (SetProperty(ref _isEnabled, value))
                    _commit(value);
            }
        }

        public EffectToggleItem(string label, string tooltip, string iconUri, bool initialValue, Action<bool> commit)
        {
            Label    = label;
            Tooltip  = tooltip;
            _commit  = commit;
            _isEnabled = initialValue;

            if (!string.IsNullOrEmpty(iconUri))
            {
                using var stream = AssetLoader.Open(new Uri(iconUri));
                IconBitmap = new Bitmap(stream);
            }
        }
    }
}
