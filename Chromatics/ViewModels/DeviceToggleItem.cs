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
            try
            {
                if (value)
                    Commit(await _enableAsync());
                else
                {
                    _disable();
                    Commit(false);
                }
            }
            catch (Exception ex)
            {
                // The property setter starts this task and never awaits it, so
                // anything escaping here becomes an UnobservedTaskException the
                // finalizer rethrows as a crash (CHROMATICS-1G, a vendor DLL
                // blocked by App Control). Report it and put the toggle back
                // where the user found it.
                //
                // Reported here rather than in each provider's delegate: only
                // the MakeDeviceToggle providers route through the guard, so
                // the hand-built toggles (Logitech, OpenRGB, Hue, LIFX and the
                // rest) would otherwise show a raw loader message instead of
                // the App Control guidance.
                if (!Helpers.AssemblyLoadGuard.TryReportLoadFailure(Label, ex))
                    Core.Logger.WriteConsole(Enums.LoggerTypes.Error,
                        $"[{Label}] could not be turned {(value ? "on" : "off")}: {ex.Message}");

                // Guarded: a subscriber throwing on the property-changed
                // notification would escape this catch and land right back in
                // the unobserved-task crash the method exists to prevent.
                try { Commit(!value); }
                catch (Exception commitEx)
                {
                    Core.Logger.WriteConsole(Enums.LoggerTypes.Error,
                        $"[{Label}] could not be restored to its previous state: {commitEx.Message}");
                }
            }
        }

        private void Commit(bool state)
        {
            _suspendCommit = true;
            try
            {
                _isEnabled = state;
                OnPropertyChanged(nameof(IsEnabled));
            }
            finally
            {
                _suspendCommit = false;
            }
        }
    }
}
