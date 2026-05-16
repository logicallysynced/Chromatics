using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Chromatics.Layers;
using Chromatics.ViewModels.Mapping;
using System;

namespace Chromatics.Views.Mapping
{
    // Drag-to-reposition for non-keyboard keycaps. Keyboards use a fixed row
    // layout and don't allow drag. For other device types we stash the start
    // pointer position + initial X/Y on PointerPressed, update X/Y live on
    // PointerMoved, then commit to MappingLayers on PointerReleased so the
    // position survives app restart.
    //
    // Handlers are registered via AddHandler with handledEventsToo:true because
    // the keycap Button marks PointerPressed as handled during its own click
    // tracking — XAML-attached handlers with the default handledEventsToo=false
    // never fire. Catching on the UserControl also lets us observe the move
    // and release events that Avalonia routes through the captured Button's
    // ancestor chain.
    public partial class VirtualDeviceView : UserControl
    {
        private KeycapViewModel _dragKeycap;
        private KeycapViewModel _clickedKeycap;
        private Button _dragButton;
        private Point _dragStartPoint;
        private double _dragStartKeycapX;
        private double _dragStartKeycapY;
        private bool _dragged;
        private bool _firstAttach = true;

        public VirtualDeviceView()
        {
            InitializeComponent();

            AddHandler(PointerPressedEvent,  OnAnyPointerPressed,  RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            AddHandler(PointerMovedEvent,    OnAnyPointerMoved,    RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            AddHandler(PointerReleasedEvent, OnAnyPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);

            AttachedToVisualTree += OnAttached;
        }

        // See LayerListView.OnAttached: force the keycap ItemsControl to
        // regenerate its containers on first visual-tree attach. Same class of
        // Avalonia bug — an ItemsControl bound to a pre-populated collection
        // inside an initially-deselected TabItem can come up with empty
        // containers until a full re-realisation.
        private void OnAttached(object sender, VisualTreeAttachmentEventArgs e)
        {
            if (!_firstAttach) return;
            _firstAttach = false;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var items = this.FindControl<ItemsControl>("KeycapItems");
                if (items == null) return;
                if (DataContext is not VirtualDeviceViewModel vm) return;
                items.ItemsSource = null;
                items.ItemsSource = vm.Keycaps;
            }, Avalonia.Threading.DispatcherPriority.Background);
        }

        private VirtualDeviceViewModel DeviceVm => DataContext as VirtualDeviceViewModel;

        private static Button FindKeycapButton(object source)
        {
            if (source is not Visual v) return null;
            var candidate = v as Button ?? v.FindAncestorOfType<Button>();
            return candidate != null && candidate.Classes.Contains("keycap") ? candidate : null;
        }

        private void OnAnyPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (_dragKeycap != null) return;

            var btn = FindKeycapButton(e.Source);
            if (btn == null) return;
            if (btn.DataContext is not KeycapViewModel keycap) return;

            // Track the pressed keycap for pick-mode click delivery regardless
            // of whether this device supports drag repositioning.
            _clickedKeycap = keycap;

            var device = DeviceVm;
            if (device == null || !device.SupportsDragReposition || !device.IsDraggable) return;

            var items = this.FindControl<ItemsControl>("KeycapItems");
            if (items == null) return;

            _dragKeycap = keycap;
            _dragButton = btn;
            _dragStartPoint = e.GetPosition(items);
            _dragStartKeycapX = keycap.X;
            _dragStartKeycapY = keycap.Y;
            _dragged = false;
        }

        private void OnAnyPointerMoved(object sender, PointerEventArgs e)
        {
            if (_dragKeycap == null) return;

            var items = this.FindControl<ItemsControl>("KeycapItems");
            if (items == null) return;

            // Only track while the primary button is actually held. In Avalonia
            // a stray PointerMoved can arrive after the Button's click logic
            // has released capture — without this guard we'd "drag" the key
            // around as the cursor wanders.
            var props = e.GetCurrentPoint(items).Properties;
            if (!props.IsLeftButtonPressed)
            {
                CancelDrag();
                return;
            }

            var current = e.GetPosition(items);
            var dx = current.X - _dragStartPoint.X;
            var dy = current.Y - _dragStartPoint.Y;

            // 3px deadzone so a plain click doesn't register as a drag and
            // trigger a persist on every keycap press.
            if (!_dragged && Math.Abs(dx) < 3 && Math.Abs(dy) < 3) return;
            _dragged = true;

            double maxX = Math.Max(0, (DeviceVm?.Width  ?? double.MaxValue) - _dragKeycap.Width);
            double maxY = Math.Max(0, (DeviceVm?.Height ?? double.MaxValue) - _dragKeycap.Height);
            _dragKeycap.X = Math.Clamp(_dragStartKeycapX + dx, 0, maxX);
            _dragKeycap.Y = Math.Clamp(_dragStartKeycapY + dy, 0, maxY);

            // Prevent the Button from treating this as a hover/scroll gesture
            // once we've decided it's a drag.
            e.Handled = true;
        }

        private void OnAnyPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var clickedKeycap = _clickedKeycap;
            _clickedKeycap = null;

            if (_dragKeycap == null)
            {
                // No drag was in progress — this is a plain click. If a layer is
                // in edit mode, forward the led to MappingViewModel's PickKey.
                if (clickedKeycap != null)
                    DeviceVm?.InvokePickKey(clickedKeycap.LedType);
                return;
            }

            var keycap = _dragKeycap;
            var device = DeviceVm;
            var wasDragged = _dragged;

            _dragKeycap = null;
            _dragButton = null;
            _dragged = false;

            if (!wasDragged)
            {
                // Drag tracking started but never left the deadzone — treat as click.
                if (clickedKeycap != null)
                    DeviceVm?.InvokePickKey(clickedKeycap.LedType);
                return;
            }

            if (device == null) return;

            MappingLayers.SetDeviceKeyPosition(device.DeviceId, keycap.LedType, keycap.X, keycap.Y);
            // Save off the UI thread. The first save after a device's layout
            // mutates touches Newtonsoft reflection paths that haven't been
            // JIT'd yet (Dictionary<LedId, DeviceKeyPosition>), producing a
            // visible stutter on the first release. Subsequent saves reuse
            // cached contracts and are instant — and we don't need to block
            // the UI thread for either case since there's no read-back.
            System.Threading.Tasks.Task.Run(() => MappingLayers.SaveMappings());
            e.Handled = true;
        }

        private void CancelDrag()
        {
            _clickedKeycap = null;
            _dragKeycap = null;
            _dragButton = null;
            _dragged = false;
        }

        private void OnResetBrightnessClick(object sender, RoutedEventArgs e)
        {
            DeviceVm?.ResetBrightness();
        }

        private async void OnCopyLayersClick(object sender, RoutedEventArgs e)
        {
            var device = DeviceVm;
            if (device == null) return;
            var owner = this.FindAncestorOfType<Window>();
            if (owner == null) return;

            // Live device dictionary from the RGB surface — the dialog
            // filters its destination list against this so newly-attached
            // devices appear without needing a Mappings-tab refresh.
            var connected = Chromatics.Core.RGBController.GetLiveDevices();

            var dlg = new Chromatics.Views.Dialogs.CopyLayersDialog(connected, device.DeviceId);
            await dlg.ShowDialog(owner);

            if (dlg.Applied)
            {
                // Let the Mappings tab pick up the freshly-created layers
                // on the destination device. The layer list view binds
                // off MappingLayers.GetLayers and rebuilds on RefreshLayers.
                if (owner.DataContext is Chromatics.ViewModels.Mapping.MappingViewModel mvm)
                {
                    try { mvm.RefreshLayers(); } catch { /* best-effort */ }
                }
            }
        }

        private async void OnResetLayoutClick(object sender, RoutedEventArgs e)
        {
            var device = DeviceVm;
            if (device == null) return;

            var owner = this.FindAncestorOfType<Window>();
            if (owner == null) return;

            var confirmed = await ShowConfirmDialog(owner,
                "Reset key positions?",
                $"This will move all keys on \"{device.DeviceName}\" back to their default grid positions. This cannot be undone.");

            if (!confirmed) return;

            device.ResetOverrides();
        }

        // Lightweight inline confirmation dialog — avoids adding a NuGet dep
        // just for a single Yes/No prompt. Returns true when user clicks Yes.
        private static async System.Threading.Tasks.Task<bool> ShowConfirmDialog(
            Window owner, string title, string message)
        {
            var result = false;

            var dialog = new Window
            {
                Title          = title,
                Width          = 380,
                Height         = 160,
                CanResize      = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent  = SizeToContent.Height,
            };

            var yesBtn = new Button { Content = "Reset", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
            var noBtn  = new Button { Content = "Cancel", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };

            yesBtn.Click += (_, _) => { result = true;  dialog.Close(); };
            noBtn.Click  += (_, _) => { result = false; dialog.Close(); };

            dialog.Content = new StackPanel
            {
                Margin    = new Thickness(20),
                Spacing   = 16,
                Children  =
                {
                    new TextBlock
                    {
                        Text        = message,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing     = 8,
                        Children    = { noBtn, yesBtn },
                    }
                }
            };

            await dialog.ShowDialog(owner);
            return result;
        }
    }
}
