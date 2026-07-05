using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.Devices.Nanoleaf;
using Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.Protocol;
using Chromatics.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.ViewModels
{
    // Adoption dialog VM: discovers controllers, walks per-controller pairing
    // (hold the button -> poll the token endpoint), supports manual-IP add,
    // and lets the user remove already-paired controllers. On Save the dialog
    // reads back every row that holds a token.
    public partial class NanoleafAdoptionDialogViewModel : ViewModelBase, IDisposable
    {
        public ObservableCollection<NanoleafControllerItem> Controllers { get; } = new();

        [ObservableProperty] private string _statusText;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _manualIp;

        public IRelayCommand AddByIpCommand { get; }

        public NanoleafAdoptionDialogViewModel()
        {
            // The async lambda runs as async void inside RelayCommand -
            // faults must stay inside or they crash the app.
            AddByIpCommand = new RelayCommand(async () =>
            {
                try { await AddByIpAsync(); }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Error, $"[Nanoleaf] add-by-IP failed: {ex.Message}");
                }
            });
            LocalizationService.Instance.PropertyChanged += OnLocaleChanged;
            UpdateStatus();
        }

        // Seed already-paired controllers, then run a discovery sweep and
        // merge in anything new that isn't already listed. IsBusy resets in
        // finally - a fault anywhere in here must not leave the dialog
        // spinning forever.
        public async Task StartDiscoveryAsync(IEnumerable<NanoleafAdoptedDevice> alreadyPaired, CancellationToken ct)
        {
            try
            {
                IsBusy = true;
                UpdateStatus();

                Controllers.Clear();
                foreach (var d in alreadyPaired ?? Enumerable.Empty<NanoleafAdoptedDevice>())
                {
                    if (d == null) continue;
                    Controllers.Add(NanoleafControllerItem.FromPaired(d, this));
                }

                try
                {
                    var found = await NanoleafDiscovery.DiscoverAsync(TimeSpan.FromSeconds(3), ct).ConfigureAwait(true);
                    foreach (var c in found)
                    {
                        string ip = c.Endpoint?.Address.ToString();
                        if (ip == null) continue;
                        if (Controllers.Any(x => x.Ip == ip)) continue;
                        Controllers.Add(NanoleafControllerItem.FromDiscovered(c, this));
                    }
                }
                catch { /* discovery is best-effort */ }
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Error, $"[Nanoleaf] adoption dialog discovery failed: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                UpdateStatus();
            }
        }

        private async Task AddByIpAsync()
        {
            var ip = (ManualIp ?? "").Trim();
            if (string.IsNullOrEmpty(ip) || !IPAddress.TryParse(ip, out _)) return;
            if (Controllers.Any(x => x.Ip == ip)) return;

            IsBusy = true;
            try
            {
                var probe = await NanoleafDiscovery.ProbeAsync(ip, 16021, TimeSpan.FromSeconds(2)).ConfigureAwait(true);
                var stub = probe ?? new NanoleafDiscoveredController { Label = ip, Endpoint = new IPEndPoint(IPAddress.Parse(ip), 16021) };
                Controllers.Add(NanoleafControllerItem.FromDiscovered(stub, this));
                ManualIp = "";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Runs the pairing poll for one controller: repeatedly POST /new for
        // ~30 seconds until the controller (in pairing mode) returns a token.
        // try/finally keeps IsBusy honest and the catch recovers the row -
        // otherwise a fault mid-poll leaves the button disabled on the
        // "hold the power button" message with the spinner stuck.
        internal async Task PairAsync(NanoleafControllerItem item)
        {
            item.SetPairing(LocalizationService.Instance["Hold the power button on this controller until it flashes..."]);
            IsBusy = true;

            try
            {
                string token = null;
                var deadline = DateTime.UtcNow.AddSeconds(30);
                while (DateTime.UtcNow < deadline && token == null)
                {
                    token = await NanoleafRestClient.PairAsync(item.Ip, item.Port).ConfigureAwait(true);
                    if (token == null) await Task.Delay(1000).ConfigureAwait(true);
                }

                if (token == null)
                {
                    item.SetPairFailed(LocalizationService.Instance["Pairing timed out. Try again and hold the button until the lights flash."]);
                    return;
                }

                // Resolve identity + panel count now that we're authorised.
                try
                {
                    var rest = new NanoleafRestClient(item.Ip, item.Port, token);
                    var state = await rest.GetStateAsync().ConfigureAwait(true);
                    item.SetPaired(token, state);
                }
                catch
                {
                    item.SetPaired(token, null);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Error, $"[Nanoleaf] pairing failed for {item.Ip}: {ex.Message}");
                item.SetPairFailed(LocalizationService.Instance["Pairing failed. Try again."]);
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal void RemovePaired(NanoleafControllerItem item)
        {
            item.ClearPairing();
        }

        // Read back everything holding a token.
        public List<NanoleafAdoptedDevice> GetAdopted()
        {
            return Controllers
                .Where(c => !string.IsNullOrEmpty(c.AuthToken))
                .Select(c => new NanoleafAdoptedDevice
                {
                    Id = c.Id,
                    Label = c.Label,
                    LastIp = c.Ip,
                    Port = c.Port,
                    AuthToken = c.AuthToken,
                    Model = c.Model,
                    Firmware = c.Firmware,
                    PanelCount = c.PanelCount,
                })
                .ToList();
        }

        private void OnLocaleChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocalizationService.Version)) UpdateStatus();
        }

        private void UpdateStatus()
        {
            StatusText = IsBusy
                ? LocalizationService.Instance["Searching for Nanoleaf controllers on your network..."]
                : Controllers.Count == 0
                    ? LocalizationService.Instance["No Nanoleaf controllers found. Check they are powered on and on the same network, or add one by IP below."]
                    : string.Format(LocalizationService.Instance["{0} controller(s) listed."], Controllers.Count);
        }

        partial void OnIsBusyChanged(bool value) => UpdateStatus();

        public void Dispose()
        {
            LocalizationService.Instance.PropertyChanged -= OnLocaleChanged;
        }
    }

    // One row in the dialog. Wraps either a paired controller or a discovered
    // (unpaired) one, and exposes an action button whose label/command flip
    // between "Pair" and "Remove".
    public partial class NanoleafControllerItem : ObservableObject
    {
        private readonly NanoleafAdoptionDialogViewModel _owner;

        public string Id { get; private set; }
        public string Ip { get; private set; }
        public int Port { get; private set; } = 16021;

        [ObservableProperty] private string _label;
        [ObservableProperty] private string _subText;
        [ObservableProperty] private string _actionLabel;
        [ObservableProperty] private bool _actionEnabled = true;
        [ObservableProperty] private string _authToken;

        public string Model { get; private set; }
        public string Firmware { get; private set; }
        public int PanelCount { get; private set; }

        public IRelayCommand ActionCommand { get; }

        private NanoleafControllerItem(NanoleafAdoptionDialogViewModel owner)
        {
            _owner = owner;
            ActionCommand = new RelayCommand(OnAction);
        }

        public static NanoleafControllerItem FromPaired(NanoleafAdoptedDevice d, NanoleafAdoptionDialogViewModel owner)
        {
            var item = new NanoleafControllerItem(owner)
            {
                Id = d.Id,
                Ip = d.LastIp,
                Port = d.Port > 0 ? d.Port : 16021,
                Label = string.IsNullOrEmpty(d.Label) ? d.LastIp : d.Label,
                Model = d.Model,
                Firmware = d.Firmware,
                PanelCount = d.PanelCount,
                AuthToken = d.AuthToken,
            };
            item.SubText = string.Format(LocalizationService.Instance["Paired - {0} panels"], d.PanelCount);
            item.ActionLabel = LocalizationService.Instance["Remove"];
            return item;
        }

        public static NanoleafControllerItem FromDiscovered(NanoleafDiscoveredController c, NanoleafAdoptionDialogViewModel owner)
        {
            var item = new NanoleafControllerItem(owner)
            {
                Id = c.Id,
                Ip = c.Endpoint?.Address.ToString(),
                Port = c.Endpoint?.Port ?? 16021,
                Label = string.IsNullOrEmpty(c.Label) ? c.Endpoint?.Address.ToString() : c.Label,
                Model = c.Model,
            };
            item.SubText = item.Ip;
            item.ActionLabel = LocalizationService.Instance["Pair"];
            return item;
        }

        private async void OnAction()
        {
            try
            {
                if (!string.IsNullOrEmpty(AuthToken))
                    _owner.RemovePaired(this);
                else
                    await _owner.PairAsync(this);
            }
            catch (Exception ex)
            {
                // async void: a fault escaping here would crash the app
                Logger.WriteConsole(LoggerTypes.Error, $"[Nanoleaf] pair/remove action failed: {ex.Message}");
            }
        }

        internal void SetPairing(string message)
        {
            SubText = message;
            ActionEnabled = false;
        }

        internal void SetPairFailed(string message)
        {
            SubText = message;
            ActionEnabled = true;
        }

        internal void SetPaired(string token, NanoleafState state)
        {
            AuthToken = token;
            if (state != null)
            {
                Id = state.SerialNo ?? Id;
                Model = state.Model;
                Firmware = state.FirmwareVersion;
                PanelCount = state.Panels?.Count ?? 0;
                if (!string.IsNullOrEmpty(state.Name)) Label = state.Name;
            }
            SubText = string.Format(LocalizationService.Instance["Paired - {0} panels"], PanelCount);
            ActionLabel = LocalizationService.Instance["Remove"];
            ActionEnabled = true;
        }

        internal void ClearPairing()
        {
            AuthToken = null;
            SubText = Ip;
            ActionLabel = LocalizationService.Instance["Pair"];
            ActionEnabled = true;
        }
    }
}
