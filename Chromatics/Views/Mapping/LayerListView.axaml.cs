using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Chromatics.ViewModels.Mapping;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Chromatics.Views.Mapping
{
    public partial class LayerListView : UserControl
    {
        private const string LayerDragFormat = "chromatics/layer-drag";

        // The item currently being dragged. Kept as a field so DragLeave/Drop
        // handlers can clear its IsDragging flag even after DoDragDrop returns.
        private LayerItemViewModel _dragSource;
        private bool _firstAttach = true;

        public LayerListView()
        {
            InitializeComponent();

            // PointerPressed must tunnel because the drag-handle Border sits
            // behind other hit-testable children; bubbling would see them first.
            LayersHost.AddHandler(PointerPressedEvent, OnHostPointerPressed, RoutingStrategies.Tunnel);

            DragDrop.SetAllowDrop(LayersHost, true);
            LayersHost.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            LayersHost.AddHandler(DragDrop.DropEvent, OnDrop);

            AttachedToVisualTree += OnAttached;
        }

        // First-realisation workaround for the "layers invisible on startup" bug.
        // When the Mappings tab isn't the initial TabControl selection, this
        // UserControl only gets its visual-tree attach the first time the user
        // clicks the tab — and by then the bound ObservableCollection has
        // already been populated during Window.Opened. Avalonia's ItemsControl
        // container generator sometimes comes up empty in that order (the
        // visual tree loads with zero containers and the populated collection's
        // earlier CollectionChanged notifications were missed). Tray hide/show
        // and theme toggles "fix" it because they force a full re-realisation.
        // Rebinding ItemsSource after attach triggers the generator to run
        // against the now-populated collection.
        private void OnAttached(object sender, VisualTreeAttachmentEventArgs e)
        {
            if (!_firstAttach) return;
            _firstAttach = false;

            Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is not MappingViewModel vm) return;
                LayersHost.ItemsSource = null;
                LayersHost.ItemsSource = vm.Layers;
            }, DispatcherPriority.Background);
        }

        private async void OnHostPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var handle = FindAncestorDragHandle(e.Source as Visual);
            if (handle == null) return;
            if (handle.DataContext is not LayerItemViewModel item) return;

            var data = new DataObject();
            data.Set(LayerDragFormat, item);

            _dragSource = item;
            item.IsDragging = true;

            try
            {
                await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            }
            catch (Exception)
            {
                // DoDragDrop can throw on session abort (window losing focus mid-drag).
                // Swallow — the VM state hasn't changed, so no recovery needed.
            }
            finally
            {
                // Clear all visual drag state whether the drop landed or was
                // abandoned (esc, mouse-up off-target, window focus loss).
                ClearDragIndicators();
            }
        }

        // Handles taps on the layer card body to toggle selection (key highlight
        // on the virtual device). Taps that originate inside a Button, ComboBox,
        // or CheckBox are ignored — those controls own their own interactions.
        private void OnLayerCardTapped(object sender, TappedEventArgs e)
        {
            if (IsInsideInteractiveControl(e.Source as Visual)) return;
            if ((sender as Control)?.DataContext is not LayerItemViewModel vm) return;
            if (DataContext is not MappingViewModel mappingVm) return;
            mappingVm.SelectLayer(vm.LayerId);
            e.Handled = true;
        }

        // Handles taps in the empty area of the list border (outside any card).
        // Tapped events from layer cards are marked handled so they don't bubble
        // here. Clears selection unless a layer is currently in edit mode.
        private void OnOuterAreaTapped(object sender, TappedEventArgs e)
        {
            if (DataContext is not MappingViewModel vm) return;
            if (vm.Layers.Any(l => l.IsEditing)) return;
            vm.ClearLayerSelection();
        }

        // Shows a confirmation dialog then removes the layer if confirmed.
        // Uses Click instead of Command so the dialog can own the async flow
        // without the ViewModel needing access to a Window.
        private async void OnDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Control)?.DataContext is not LayerItemViewModel layerVm) return;
            if (DataContext is not MappingViewModel mappingVm) return;

            var owner = this.FindAncestorOfType<Window>();
            if (owner == null) return;

            bool confirmed = await ShowDeleteConfirmDialog(owner, layerVm);
            if (confirmed) mappingVm.RemoveLayer(layerVm.LayerId);
        }

        private static bool IsInsideInteractiveControl(Visual source)
        {
            var v = source;
            while (v != null)
            {
                if (v is Button || v is ComboBox || v is CheckBox) return true;
                v = v.GetVisualParent();
            }
            return false;
        }

        private static async Task<bool> ShowDeleteConfirmDialog(Window owner, LayerItemViewModel layer)
        {
            var result = false;

            var dialog = new Window
            {
                Title = "Delete Layer",
                Width = 360,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.Height,
            };

            var yesBtn = new Button { Content = "Delete", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
            var noBtn  = new Button { Content = "Cancel", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };

            yesBtn.Click += (_, _) => { result = true;  dialog.Close(); };
            noBtn.Click  += (_, _) => { result = false; dialog.Close(); };

            dialog.Content = new StackPanel
            {
                Margin  = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text        = $"Delete layer {layer.BadgeText}? This cannot be undone.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new StackPanel
                    {
                        Orientation         = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing             = 8,
                        Children            = { noBtn, yesBtn },
                    }
                }
            };

            await dialog.ShowDialog(owner);
            return result;
        }

        private static Control FindAncestorDragHandle(Visual source)
        {
            while (source != null)
            {
                if (source is Control c && c.Classes.Contains("dragHandle"))
                    return c;
                source = source.GetVisualParent();
            }
            return null;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.Contains(LayerDragFormat))
            {
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;

            // Paint an insertion indicator on the item the cursor currently
            // sits over so the user can see *where* the drop will land
            // without having to release first.
            if (DataContext is not MappingViewModel vm) return;
            int targetIndex = ComputeDropIndex(e.GetPosition(LayersHost), vm.Layers.Count);
            UpdateDropIndicators(vm, targetIndex);
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (DataContext is not MappingViewModel vm) return;
            if (e.Data.Get(LayerDragFormat) is not LayerItemViewModel source) return;

            int from = vm.Layers.IndexOf(source);
            if (from < 0) { ClearDragIndicators(); return; }

            int to = ComputeDropIndex(e.GetPosition(LayersHost), vm.Layers.Count);
            ClearDragIndicators();

            if (to < 0 || to == from) return;

            vm.MoveLayer(from, to);
            e.Handled = true;
        }

        // Set exactly one layer's IsDropTargetAbove (or IsDropTargetBelow for
        // the last slot) and clear every other layer's flags. Cheap — the
        // layer list is tiny (≤20 items typically).
        private void UpdateDropIndicators(MappingViewModel vm, int targetIndex)
        {
            int last = vm.Layers.Count - 1;
            for (int i = 0; i < vm.Layers.Count; i++)
            {
                var layer = vm.Layers[i];
                bool above = targetIndex == i && targetIndex <= last;
                bool below = targetIndex > last && i == last;
                if (layer.IsDropTargetAbove != above) layer.IsDropTargetAbove = above;
                if (layer.IsDropTargetBelow != below) layer.IsDropTargetBelow = below;
            }
        }

        private void ClearDragIndicators()
        {
            if (_dragSource != null)
            {
                _dragSource.IsDragging = false;
                _dragSource = null;
            }
            if (DataContext is not MappingViewModel vm) return;
            foreach (var layer in vm.Layers)
            {
                if (layer.IsDropTargetAbove) layer.IsDropTargetAbove = false;
                if (layer.IsDropTargetBelow) layer.IsDropTargetBelow = false;
            }
        }

        // Walks the generated item containers to find the one whose vertical
        // midpoint the cursor sits above. Above-center -> insert at that index,
        // below-center -> after it. Falls back to the end when the cursor is
        // past every container.
        private int ComputeDropIndex(Point positionInHost, int itemCount)
        {
            for (int i = 0; i < itemCount; i++)
            {
                var container = LayersHost.ContainerFromIndex(i);
                if (container == null) continue;

                var topLeft = container.TranslatePoint(new Point(0, 0), LayersHost);
                if (topLeft == null) continue;

                double centerY = topLeft.Value.Y + container.Bounds.Height / 2;
                if (positionInHost.Y < centerY)
                    return i;
            }
            return itemCount - 1;
        }
    }
}
