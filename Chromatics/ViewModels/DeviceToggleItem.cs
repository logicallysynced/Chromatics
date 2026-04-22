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
                _ = SetIsEnabledAsync(value);
            }
        }

        public DeviceToggleItem(string label, string tooltipKey, bool initialValue, Func<Task<bool>> enableAsync, Action disable)
        {
            Label = label;
            _tooltipKey = tooltipKey;
            _isEnabled = initialValue;
            _enableAsync = enableAsync;
            _disable = disable;

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
