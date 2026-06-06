using Chromatics.Localization;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Chromatics.ViewModels
{
    public sealed class DeviceToggleItem : ViewModelBase
    {
        // Returning a bool from EnableAsync lets providers veto an enable
        // (e.g. the Hue dialog rejecting a bad bridge IP). If false, IsEnabled
        // reverts and settings aren't flipped.
        private readonly Func<Task<bool>> _enableAsync;
        private readonly Action _disable;
        private readonly string _tooltipKey;
        private bool _isEnabled;
        private bool _suspendCommit;

        public string Label { get; }
        public string Tooltip => LocalizationService.Instance[_tooltipKey];

        // UI-availability flag (separate from the on/off state in IsEnabled).
        // Bound to the ToggleButton's IsEnabled in the Settings view so a
        // toggle can be presented as greyed-out + unclickable when the host
        // OS doesn't support the underlying provider — e.g. Dynamic Lighting
        // on Windows 10. Default true keeps every other toggle unchanged.
        public bool IsAvailable { get; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value) return;
                if (_suspendCommit)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                    return;
                }
                // Optimistic update: reflect the user's click immediately so
                // the binding sees a real state transition. Without this the
                // setter never wrote _isEnabled or fired PropertyChanged on
                // the way in, so when SetIsEnabledAsync rejected the change
                // and wrote _isEnabled = false, the value matched what the
                // binding had already cached and the UI didn't refresh
                // until the toggle was re-realised (e.g. tab navigation).
                _isEnabled = value;
                OnPropertyChanged();
                _ = SetIsEnabledAsync(value);
            }
        }

        public DeviceToggleItem(string label, string tooltipKey, bool initialValue, Func<Task<bool>> enableAsync, Action disable, bool isAvailable = true)
        {
            Label = label;
            _tooltipKey = tooltipKey;
            _isEnabled = initialValue;
            _enableAsync = enableAsync;
            _disable = disable;
            IsAvailable = isAvailable;

            LocalizationService.Instance.PropertyChanged += OnLocaleVersionChanged;
        }

        private void OnLocaleVersionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocalizationService.Version))
                OnPropertyChanged(nameof(Tooltip));
        }

        private async Task SetIsEnabledAsync(bool value)
        {
            if (value)
            {
                var accepted = await _enableAsync();
                _suspendCommit = true;
                try
                {
                    _isEnabled = accepted;
                    OnPropertyChanged(nameof(IsEnabled));
                }
                finally
                {
                    _suspendCommit = false;
                }
            }
            else
            {
                _disable();
                _suspendCommit = true;
                try
                {
                    _isEnabled = false;
                    OnPropertyChanged(nameof(IsEnabled));
                }
                finally
                {
                    _suspendCommit = false;
                }
            }
        }
    }
}
