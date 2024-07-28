using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions;
using Chromatics.Helpers;
using Chromatics.Layers;
using Chromatics.Models;
using Gma.System.MouseKeyHook;
using HidSharp;
using MetroFramework;
using MetroFramework.Components;
using MetroFramework.Controls;
using OpenRGB.NET;
using RGB.NET.Core;
using Sanford.Multimedia;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static Chromatics.Models.VirtualDevice;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using MethodInvoker = System.Windows.Forms.MethodInvoker;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace Chromatics.Forms
{
    public partial class Uc_Mappings : UserControl
    {
        public static Uc_Mappings Instance { get; private set; }
        public static event EventHandler DeviceAdded;
        public static event EventHandler DeviceRemoved;

        public MetroTabControl TabManager { get; set; }
        private IKeyboardMouseEvents keyController;

        private List<Pn_LayerDisplay> _layers = new List<Pn_LayerDisplay>();
        private List<KeyButton> _currentSelectedKeys = new List<KeyButton>();
        private Dictionary<Guid, VirtualDevice> _deviceVirtualDeviceMap;
        private Dictionary<int, LedId> currentKeySelection = new Dictionary<int, LedId>();
        private int _layerPad = 5;
        private MetroToolTip tt_mappings;
        private LayerType selectedAddType;
        private RGBDeviceType selectedDevice;
        private Pn_LayerDisplay currentlyEditing;
        private Pn_LayerDisplay currentlySelected;
        private bool init;
        private bool IsAddingLayer;

        public static void OnDeviceAdded(EventArgs e)
        {
            DeviceAdded?.Invoke(null, e);
        }

        public static void OnDeviceRemoved(EventArgs e)
        {
            DeviceRemoved?.Invoke(null, e);
        }

        public Uc_Mappings()
        {
            InitializeComponent();
                        
            Instance = this;
            tlp_base.Size = this.Size;
            _deviceVirtualDeviceMap = new Dictionary<Guid, VirtualDevice>();

            this.flp_layers.DragEnter += new DragEventHandler(flp_layers_DragEnter);
            this.flp_layers.DragDrop += new DragEventHandler(flp_layers_DragDrop);
            GameController.jobChanged += gameJobChanged;

            // Register event handlers for device changes
            DeviceAdded += HandleDeviceAdded;
            DeviceRemoved += HandleDeviceRemoved;

            // Initialize the form
            this.Load += new EventHandler(OnLoad);
        }

        private void OnLoad(object sender, EventArgs e)
        {
            //Create tooltop manager
            tt_mappings = new MetroToolTip
            {
                ToolTipIcon = ToolTipIcon.Info,
                IsBalloon = true,
                ShowAlways = true
            };

            //Disable Horizontal Scrolling
            flp_layers.AutoScroll = false;
            flp_layers.HorizontalScroll.Maximum = 0;
            flp_layers.VerticalScroll.Visible = false;
            flp_layers.AutoScroll = true;

            //Enumerate LayerTypes into Add Layer Combobox
            foreach (Enum lt in Enum.GetValues(typeof(LayerType)))
            {
                var name = EnumExtensions.GetAttribute<DisplayAttribute>(lt).Name;
                var item = new ComboboxItem { Value = lt, Text = name };

                if ((LayerType)item.Value == LayerType.BaseLayer || (LayerType)item.Value == LayerType.EffectLayer)
                    continue;

                cb_addlayer.Items.Add(item);
            }

            cb_addlayer.SelectedIndex = 0;
            selectedAddType = LayerType.DynamicLayer;

            //Enumerate LayerModes into Mode Combobox
            foreach (Enum lt in Enum.GetValues(typeof(LayerModes)))
            {
                var name = EnumExtensions.GetAttribute<DisplayAttribute>(lt).Name;
                var item = new ComboboxItem { Value = lt, Text = name };

                cb_changemode.Items.Add(item);
            }

            cb_changemode.SelectedIndex = 0;

            //Check for existing mappings and load, or else create defaults
            if (!init)
            {
                if (MappingLayers.LoadMappings())
                {
                    var layercount = MappingLayers.CountLayers();
                    Logger.WriteConsole(LoggerTypes.System, $"Loaded {layercount} layers from layers.chromatics3");

                    ChangeDeviceType();

                }
                else
                {
                    Logger.WriteConsole(LoggerTypes.System, @"No layer file found. Creating default layers..");
                    //SaveLayers(true);
                }

                InitializeAndVisualize();
                VisualiseLayers();
            }
            

            //Add tooltips
            tt_mappings.SetToolTip(this.cb_addlayer, @"Add New Layer of selected type");
            tt_mappings.SetToolTip(this.cb_deviceselect, @"Change to another device");
            tt_mappings.SetToolTip(this.btn_preview, @"Show virtual lighting on buttons");
            tt_mappings.SetToolTip(this.btn_clearselection, @"Clear all keys on layer");
            tt_mappings.SetToolTip(this.btn_reverseselection, @"Reverse keys on layer");
            tt_mappings.SetToolTip(this.btn_undoselection, @"Undo key selection on layer");
            tt_mappings.SetToolTip(this.btn_togglebleed, @"For layers which have a negative colour, allow lower layers to bleed through instead of showing the negative colour.");
            tt_mappings.SetToolTip(this.cb_changemode, @"Change the layer mode." + Environment.NewLine + @"Interpolate: Shows the layer as a bar on your device." + Environment.NewLine + @"Fade: Fades the colour of the RGB keys.");

            //Handle Events
            //this.TabManager.Selecting += new TabControlCancelEventHandler(mT_TabManager_Selecting);
            this.TabManager.Selected += new TabControlEventHandler(mT_TabManager_Selected);

            if (cb_deviceselect.Items.Count == 0)
            {
                AddNoDevicesDetectedItem();
            }
            else
            {
                RemoveNoDevicesDetectedItem();
            }

            //Set init to true
            init = true;
        }

        private void HandleDeviceAdded(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(HandleDeviceAdded), sender, e);
                return;
            }

            EnumerateAndAddDevices();
        }

        private void HandleDeviceRemoved(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(HandleDeviceRemoved), sender, e);
                return;
            }

            EnumerateAndRemoveDevices();
        }

        private void EnumerateAndAddDevices()
        {
            var connectedDevices = RGBController.GetLiveDevices();

            if (connectedDevices != null && connectedDevices.Count > 0)
            {
                // Remove "No Devices Detected" if it exists
                RemoveNoDevicesDetectedItem();

                foreach (var device in connectedDevices)
                {
                    if (_deviceVirtualDeviceMap.ContainsKey(device.Key))
                    {
                        // Device already exists, skip adding
                        continue;
                    }

                    var item = new ComboboxItem { Value = device.Key, Text = device.Value.DeviceInfo.DeviceName };
                    cb_deviceselect.Items.Add(item);

                    Debug.WriteLine($"Added device {device.Value.DeviceInfo.DeviceName} with GUID {device.Key}");

                    VirtualDevice virtualDevice;

                    if (device.Value.DeviceInfo.DeviceType == RGBDeviceType.Keyboard)
                    {
                        virtualDevice = new Uc_VirtualKeyboard();
                        virtualDevice._deviceId = device.Key;
                        virtualDevice._deviceType = device.Value.DeviceInfo.DeviceType;
                    }
                    else
                    {
                        virtualDevice = new Uc_VirtualOtherController();
                        virtualDevice._deviceId = device.Key;
                        virtualDevice._deviceType = device.Value.DeviceInfo.DeviceType;
                    }

                    virtualDevice.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                    virtualDevice.Dock = DockStyle.Top;
                    virtualDevice.MinimumSize = new Size(1200, 300);
                    virtualDevice.AutoSize = false;
                    tlp_frame.Controls.Add(virtualDevice, 0, 0);

                    virtualDevice._OnKeycapPressed += new EventHandler(OnKeyCapPressed);

                    _deviceVirtualDeviceMap[device.Key] = virtualDevice;

                    // Manually trigger the OnLoad event
                    virtualDevice.InitializeDevice();
                }

                // Remove devices that are no longer connected
                var disconnectedDeviceKeys = _deviceVirtualDeviceMap.Keys.Except(connectedDevices.Keys).ToList();
                foreach (var key in disconnectedDeviceKeys)
                {
                    if (_deviceVirtualDeviceMap.TryGetValue(key, out var virtualDevice))
                    {
                        virtualDevice._OnKeycapPressed -= OnKeyCapPressed;
                        tlp_frame.Controls.Remove(virtualDevice);
                        virtualDevice.Dispose();
                        _deviceVirtualDeviceMap.Remove(key);
                    }
                }

                // Select the first item if no item is selected
                if (cb_deviceselect.Items.Count > 0 && cb_deviceselect.SelectedIndex == -1)
                {
                    cb_deviceselect.SelectedIndex = 0;
                }

                // Ensure all devices and layers are visualized after enumeration
                VisualiseLayers();
            }

            if (cb_deviceselect.Items.Count == 0)
            {
                AddNoDevicesDetectedItem();
            }

            CreateDefaults();
        }

        private void EnumerateAndRemoveDevices()
        {
            var connectedDevices = RGBController.GetLiveDevices();
            var currentSelectedDevice = cb_deviceselect.SelectedItem as ComboboxItem;

            // Remove devices that are no longer connected
            var disconnectedDeviceKeys = _deviceVirtualDeviceMap.Keys.Except(connectedDevices?.Keys ?? Enumerable.Empty<Guid>()).ToList();
            foreach (var key in disconnectedDeviceKeys)
            {

                Debug.WriteLine($"Removed device with GUID {key}");
                if (_deviceVirtualDeviceMap.TryGetValue(key, out var virtualDevice))
                {
                    virtualDevice._OnKeycapPressed -= OnKeyCapPressed;
                    tlp_frame.Controls.Remove(virtualDevice);
                    virtualDevice.Dispose();
                    _deviceVirtualDeviceMap.Remove(key);
                }

                // Remove the device from the ComboBox
                var itemToRemove = cb_deviceselect.Items.OfType<ComboboxItem>().FirstOrDefault(item => item.Value.Equals(key));
                if (itemToRemove != null)
                {
                    cb_deviceselect.Items.Remove(itemToRemove);
                }

                // Change selected device if the current selected device is being removed
                if (currentSelectedDevice != null && currentSelectedDevice.Value.Equals(key))
                {
                    cb_deviceselect.SelectedIndex = cb_deviceselect.Items.Count > 0 ? 0 : -1;
                    ChangeDeviceType();
                }
            }

            if (cb_deviceselect.Items.Count == 0)
            {
                AddNoDevicesDetectedItem();
            }
            else
            {
                RemoveNoDevicesDetectedItem();
            }
        }


        private void AddNoDevicesDetectedItem()
        {
            var noDevicesItem = new ComboboxItem { Value = Guid.Empty, Text = "No Devices Detected" };
            cb_deviceselect.Items.Add(noDevicesItem);
            cb_deviceselect.SelectedItem = noDevicesItem;
            cb_deviceselect.Enabled = false;
        }

        private void RemoveNoDevicesDetectedItem()
        {
            var noDevicesItem = cb_deviceselect.Items.OfType<ComboboxItem>().FirstOrDefault(item => item.Text == "No Devices Detected");
            if (noDevicesItem != null)
            {
                cb_deviceselect.Items.Remove(noDevicesItem);
            }
            cb_deviceselect.Enabled = true;
        }


        public void flp_layers_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        public static ComboboxItem GetActiveDevice()
        {

            return Instance.cb_deviceselect.SelectedItem as ComboboxItem;
        }

        public void flp_layers_DragDrop(object sender, DragEventArgs e)
        {
            var data = (Pn_LayerDisplay)e.Data.GetData(typeof(Pn_LayerDisplay));
            var _destination = (FlowLayoutPanel)sender;
            var _source = (FlowLayoutPanel)data.Parent;

            if (data.LayerType == LayerType.DynamicLayer)
            {
                if (_source == _destination)
                {
                    Point p = _destination.PointToClient(new Point(e.X, e.Y));
                    var item = _destination.GetChildAtPoint(p);
                    int index = _destination.Controls.GetChildIndex(item, false);

                    if (index < 0)
                    {
                        index = _layers.Count - 2;
                    }
                    else if (index >= 0 && index <= 1)
                    {
                        index = 1;
                    }
                    else if (index >= _layers.Count - 1)
                    {
                        index = _layers.Count - 2;
                    }



                    // Add control to panel
                    _destination.Controls.Add(data);
                    data.Size = new Size(_destination.Width - _layerPad, 50);
                    data.LeftText = (flp_layers.Controls.Count - index).ToString();

                    // Reorder

                    _destination.Controls.SetChildIndex(data, index);

                    foreach (Pn_LayerDisplay layer in flp_layers.Controls)
                    {
                        var _layer = MappingLayers.GetLayer(layer.ID);
                        var i = flp_layers.Controls.Count - flp_layers.Controls.GetChildIndex(layer);

                        _layer.zindex = i;
                        MappingLayers.UpdateLayer(_layer);
                        layer.LeftText = i.ToString();


                        layer.Invalidate();
                    }

                    SaveLayers();
                }
            }
        }

        private void SaveLayers(bool bypass = false)
        {
            if (init || bypass)
            {
                MappingLayers.SaveMappings();
            }
        }

        private int AddLayer(LayerType layertype, Guid deviceId, RGBDeviceType deviceType, int id = 0, int index = 0, bool initialize = false, bool bypass = false, bool enabled = false, Dictionary<int, LedId> leds = null, int layerTypeindex = 0, bool allowBleed = false, LayerModes mode = LayerModes.Interpolate)
        {
            IsAddingLayer = true;
            var s = new Size(flp_layers.Width - _layerPad, 50);

            var pgb = new Pn_LayerDisplay
            {
                Padding = new Padding(_layerPad, _layerPad, _layerPad, _layerPad),
                LeftText = (_layers.Count + 1).ToString(),
                LayerType = layertype,
                DeviceId = deviceId, // Added DeviceId to the layer display
                Size = s,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Dock = DockStyle.Top
            };

            tt_mappings.SetToolTip(pgb.chk_enabled, "Enable/Disable Layer");
            tt_mappings.SetToolTip(pgb.cb_selector, "Select the type of Layer");
            tt_mappings.SetToolTip(pgb.btn_edit, "Toggle Edit mode for this Layer");
            tt_mappings.SetToolTip(pgb.btn_delete, "Remove this Layer");

            pgb.cb_selector.SelectedIndex = layerTypeindex;
            pgb.GotFocus += new EventHandler(OnLayerPressed);
            pgb.cb_selector.SelectedIndexChanged += new EventHandler(OnSelectedIndexChanged);
            pgb.chk_enabled.CheckedChanged += new EventHandler(OnCheckChanged);
            pgb.btn_edit.Click += new EventHandler(OnEditButtonPressed);
            pgb.btn_delete.Click += new EventHandler(OnDeleteButtonPressed);
            pgb.btn_copy.Click += new EventHandler(OnCopyButtonPressed);

            if (initialize)
            {
                var _index = index;

                if (_index == 0)
                {
                    _index = (_layers.Count + 1);
                }

                if (leds == null)
                {
                    leds = new Dictionary<int, LedId>();
                }

                pgb.ID = MappingLayers.AddLayer(MappingLayers.CountLayers(), layertype, deviceId, deviceType, layerTypeindex, _index, enabled, leds, allowBleed, mode);
                Debug.WriteLine($"Added Layer: {pgb.ID}. Index: {_index}. Enabled: {enabled}. AllowBleed: {allowBleed}. LayerTypeindex: {layerTypeindex}. Mode: {mode}. LayerType: {layertype}. Leds: {leds.Count}");

                pgb.LeftText = _index.ToString();
            }
            else
            {
                var layer = MappingLayers.GetLayer(id);
                pgb.ID = layer.layerID;
                pgb.chk_enabled.Checked = layer.Enabled;
                pgb.LeftText = (layer.zindex).ToString();

                var cb = pgb.cb_selector;
                var cb_state = cb.Enabled;

                cb.Enabled = true;
                cb.SelectedIndex = layer.layerTypeindex;
                cb.Enabled = cb_state;

                if (index > 0)
                {
                    layer.zindex = index;
                    MappingLayers.UpdateLayer(layer);
                }
            }

            _layers.Add(pgb);

            flp_layers.Controls.Add(pgb);

            IsAddingLayer = false;

            return pgb.ID;
        }

        private void CreateDefaults()
        {
            // Create Default Layers

            var connectedDevices = RGBController.GetLiveDevices();
            

            if (connectedDevices != null)
            {

                foreach (var devicePair in connectedDevices)
                {
                    var device = devicePair.Value;
                    var deviceGuid = devicePair.Key;
                    var deviceType = device.DeviceInfo.DeviceType;

                    // Skip if there are already layers for this device
                    if (MappingLayers.GetLayers().Values.Any(layer => layer.deviceGuid == deviceGuid)) continue;

                    var i = 1;

                    // Skip certain device types
                    if (deviceType == RGBDeviceType.None || deviceType == RGBDeviceType.All)
                        continue;

                    Debug.WriteLine($"Adding base layer for device: {deviceGuid}. Keys: {device.Count()}");

                    var base_i = 0;
                    var baseKeys = new Dictionary<int, LedId>();

                    foreach (var led in device)
                    {
                        if (!baseKeys.ContainsKey(base_i))
                        {
                            baseKeys.Add(base_i, led.Id);
                        }

                        base_i++;
                    }

                    // Add base layer
                    AddLayer(LayerType.BaseLayer, deviceGuid, deviceType, 0, i, true, false, true, baseKeys);
                    i++;

                    // Add dynamic layer for keyboards
                    if (deviceType == RGBDeviceType.Keyboard)
                    {
                        AddLayer(LayerType.DynamicLayer, deviceGuid, deviceType, 0, i, true, false, true);
                        i++;
                    }

                    // Add effect layer
                    AddLayer(LayerType.EffectLayer, deviceGuid, deviceType, 0, i, true, false, true, baseKeys);

                    Logger.WriteConsole(LoggerTypes.System, $"Creating default layers for device {deviceType}. Key Count: {baseKeys.Count}");
                }

                // Change device type and save layers
                
                ChangeDeviceType();

            }

            SaveLayers();
        }

        public void ChangeDeviceType()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(ChangeDeviceType));
                return;
            }

            var selectedDeviceItem = cb_deviceselect.SelectedItem as ComboboxItem;
            if (selectedDeviceItem == null || !(selectedDeviceItem.Value is Guid selectedDeviceId))
            {
                return;
            }

            var _selectedDevice = RGBController.GetLiveDevices().FirstOrDefault(d => d.Key == selectedDeviceId).Value;
            if (_selectedDevice == null)
            {
                return;
            }

            var selectedDeviceType = _selectedDevice.DeviceInfo.DeviceType;

            // Hide all virtual device controls and show the selected one
            HideAllVirtualDevices();
            ShowVirtualDevice(selectedDeviceId);

            // Suspend layout while making multiple changes
            tlp_frame.SuspendLayout();
            flp_layers.SuspendLayout();

#if DEBUG
            Debug.WriteLine($"Changing device type to {selectedDevice}. Key Count: {_selectedDevice.Count()}");
#endif

            // Disable Editing and remove layers for existing device types
            btn_clearselection.Enabled = false;
            btn_reverseselection.Enabled = false;
            btn_undoselection.Enabled = false;
            btn_togglebleed.Enabled = false;
            cb_changemode.Enabled = false;
            currentlyEditing = null;
            currentlySelected = null;

            var item = new ComboboxItem { Value = LayerModes.None, Text = EnumExtensions.GetAttribute<DisplayAttribute>(LayerModes.None).Name };

            cb_changemode.Items.Add(item);
            cb_changemode.SelectedItem = item;

            RevertButtons();

            foreach (var layer in _layers)
            {
                if (layer.editing)
                {
                    layer.editing = false;
                    layer.selected = false;
                    layer.Update();
                }

                flp_layers.Controls.Remove(layer);

                // Cleanup
                layer.GotFocus -= new EventHandler(OnLayerPressed);
                layer.cb_selector.SelectedIndexChanged -= new EventHandler(OnSelectedIndexChanged);
                layer.chk_enabled.CheckedChanged -= new EventHandler(OnCheckChanged);
                layer.btn_edit.Click -= new EventHandler(OnEditButtonPressed);
                layer.btn_delete.Click -= new EventHandler(OnDeleteButtonPressed);
                layer.btn_copy.Click -= new EventHandler(OnCopyButtonPressed);

                layer.Dispose();
            }

            tt_mappings.RemoveAll();
            tt_mappings.SetToolTip(this.cb_addlayer, "Add New Layer of selected type");
            tt_mappings.SetToolTip(this.cb_deviceselect, "Change to another device");
            rtb_layerhelper.Text = @"";

            flp_layers.Controls.Clear();
            _layers.Clear();

            // Add layers for new device types
            var _NewLayers = MappingLayers.GetLayers().Where(r => r.Value.deviceGuid.Equals(selectedDeviceId));

            foreach (var layers in _NewLayers.OrderByDescending(r => r.Value.zindex))
            {
                AddLayer(layers.Value.rootLayerType, selectedDeviceId, selectedDeviceType, layers.Key, layers.Value.zindex, false);
            }

            ResizeLayerHelpText(rtb_layerhelp);
            // Resume layout
            tlp_frame.ResumeLayout();
            flp_layers.ResumeLayout();

            VisualiseLayers();
        }

        private void HideAllVirtualDevices()
        {
            foreach (var control in tlp_frame.Controls.OfType<VirtualDevice>())
            {
                control.Visible = false;
            }
        }

        private void ShowVirtualDevice(Guid deviceId)
        {
            if (_deviceVirtualDeviceMap.TryGetValue(deviceId, out var existingVirtualDevice))
            {
                // Device already exists, just ensure it is visible
                existingVirtualDevice.Visible = true;
                InitializeAndVisualize();
                return;
            }

            var selectedDevice = RGBController.GetLiveDevices().FirstOrDefault(d => d.Key == deviceId).Value;

            if (selectedDevice != null)
            {
                VirtualDevice virtualDevice;
                if (selectedDevice.DeviceInfo.DeviceType == RGBDeviceType.Keyboard)
                {
                    virtualDevice = new Uc_VirtualKeyboard();
                    virtualDevice._deviceId = deviceId;
                    virtualDevice._deviceType = selectedDevice.DeviceInfo.DeviceType;
                }
                else
                {
                    virtualDevice = new Uc_VirtualOtherController();
                    virtualDevice._deviceId = deviceId;
                    virtualDevice._deviceType = selectedDevice.DeviceInfo.DeviceType;
                }

                virtualDevice.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                virtualDevice.Dock = DockStyle.Top;
                virtualDevice.MinimumSize = new Size(1200, 300);
                virtualDevice.AutoSize = false;
                tlp_frame.Controls.Add(virtualDevice, 0, 0);

                virtualDevice._OnKeycapPressed += new EventHandler(OnKeyCapPressed);
                virtualDevice.Visible = true;

                _deviceVirtualDeviceMap[deviceId] = virtualDevice;

                InitializeAndVisualize();
            }
        }



        private void InitializeAndVisualize()
        {

            // Call this method after controls are loaded
            foreach (Control ctrl in tlp_frame.Controls)
            {
                if (ctrl is VirtualDevice vd)
                {
                    vd.Load += (s, e) =>
                    {
                        if (vd.init)
                        {
                            VisualiseLayers();
                        }
                    };
                }
            }
        }

        public void VisualiseLayers()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(VisualiseLayers));
                return;
            }

            var selectedDeviceItem = cb_deviceselect.SelectedItem as ComboboxItem;
            if (selectedDeviceItem == null || selectedDeviceItem.Value is not Guid selectedDeviceId)
            {
                return;
            }
                        

            var layers = MappingLayers.GetLayers().Where(x => x.Value.deviceGuid == selectedDeviceId).OrderBy(x => x.Value.zindex);

            foreach (var layer in layers)
            {
                foreach (var virtualDevice in tlp_frame.Controls.OfType<VirtualDevice>())
                {
                    if (layer.Value.deviceGuid == selectedDeviceId)
                    {
                        var allKeyButtons = virtualDevice._keybuttons.ToList();
                        virtualDevice.VisualiseLayers(layers, allKeyButtons);
                    }
                }
            }
        }


        private void RevertButtons()
        {
            foreach (var button in _currentSelectedKeys)
            {
                button.BackColor = System.Drawing.Color.DarkGray;
                button.BorderCol = System.Drawing.Color.Black;
                button.RemoveCircle();
            }

            VisualiseLayers();

            currentKeySelection.Clear();
            _currentSelectedKeys.Clear();
        }

        private void OnResize(object sender, EventArgs e)
        {
            this.SuspendLayout();
            foreach (var layer in _layers)
            {
                layer.Size = new Size(flp_layers.Width - _layerPad, 50);
                layer.Update();
            }

            this.ResumeLayout(true);
        }

        private void mT_TabManager_Selected(object sender, TabControlEventArgs e)
        {
            if (IsAddingLayer) return;

            var current = (sender as TabControl).SelectedTab.Name;

            keyController = KeyController.GetKeyContoller();
            if (current == "tP_mappings") // Ensure this matches the name of your mappings tab
            {
                // Key controllers hook
                if (keyController != null)
                {
                    keyController.KeyDown += Kh_KeyDown;
                    keyController.KeyUp += Kh_KeyUp;
                }

                //VisualiseLayers();

            }
            else
            {
                // Key controllers unhook
                if (keyController != null)
                {
                    keyController.KeyDown -= Kh_KeyDown;
                    keyController.KeyUp -= Kh_KeyUp;
                }
            }
        }


        private void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            //Change Layer Type
            var obj = (MetroComboBox)sender;
            var parent = (Pn_LayerDisplay)obj.Parent;
            var id = parent.ID;

            var selectedindex = obj.SelectedIndex;

            var layer = MappingLayers.GetLayer(id);

            ResetLayerText(layer);

            layer.layerTypeindex = selectedindex;
            layer.requestUpdate = true;

            RGBController.RemoveLayerGroup(layer.layerID);
            MappingLayers.UpdateLayer(layer);

            SaveLayers();
        }

        private void OnCheckChanged(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            //Set Enabled
            var obj = (CheckBox)sender;
            var parent = (Pn_LayerDisplay)obj.Parent;
            var id = parent.ID;

            var layer = MappingLayers.GetLayer(id);
            layer.Enabled = obj.Checked;
            layer.requestUpdate = true;
            MappingLayers.UpdateLayer(layer);
            SaveLayers();
            VisualiseLayers();
        }

        private void OnLayerPressed(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;
            if (currentlyEditing != null) return;

            var obj = (Pn_LayerDisplay)sender;

            var selectedDeviceItem = cb_deviceselect.SelectedItem as ComboboxItem;
            if (selectedDeviceItem == null || selectedDeviceItem.Value is not Guid selectedDeviceId)
            {
                return;
            }

            var selectedDevice = RGBController.GetLiveDevices().FirstOrDefault(d => d.Key == selectedDeviceId).Value;

            if (selectedDevice != null)
            {
                if (currentlySelected != null)
                {
                    currentlySelected.selected = false;
                    currentlySelected = null;

                    var virtualDevice = tlp_frame.Controls.OfType<VirtualDevice>().FirstOrDefault(vd => vd.Visible);
                    if (virtualDevice != null)
                    {
                        foreach (var key in virtualDevice._keybuttons)
                        {
                            if (!key.IsEditing && key.BorderCol != System.Drawing.Color.Black)
                            {
                                key.BorderCol = System.Drawing.Color.Black;
                                key.Invalidate();
                            }
                        }
                    }
                }

                var layer = MappingLayers.GetLayer(obj.ID);

                var virtualDeviceToShow = tlp_frame.Controls.OfType<VirtualDevice>().FirstOrDefault(vd => vd.Visible);

                if (virtualDeviceToShow != null)
                {
                    foreach (var selection in layer.deviceLeds)
                    {
                        foreach (var key in virtualDeviceToShow._keybuttons)
                        {
                            if (key.KeyType == selection.Value && !key.IsEditing)
                            {
                                key.BorderCol = System.Drawing.Color.SandyBrown;
                                key.Invalidate();
                            }
                        }
                    }
                }

                if (layer.allowBleed)
                {
                    btn_togglebleed.Text = @"Bleed Enabled";
                }
                else
                {
                    btn_togglebleed.Text = @"Bleed Disabled";
                }

                ChangeLayerMode(layer);

                ResetLayerText(layer);
                ResizeLayerHelpText(rtb_layerhelp);

                obj.selected = true;
                currentlySelected = obj;
                obj.Invalidate();
            }
        }

        private void ResetLayerText(Layer layer)
        {
            if (!init) return;

            if (rtb_layerhelper.InvokeRequired)
            {
                rtb_layerhelper.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    if (layer.rootLayerType == LayerType.DynamicLayer)
                    {
                        rtb_layerhelper.Text = TextHelper.ParseLayerHelperText(((LayerDisplay)typeof(DynamicLayerType).GetField(Enum.GetName(typeof(DynamicLayerType), layer.layerTypeindex)).GetCustomAttribute(typeof(LayerDisplay))).Description);

                        if (layer.layerTypeindex == 8)
                        {
                            rtb_layerhelper.Text += $"\n{GameHelper.GetJobClassDynamicLayerDescriptions(GameController.GetCurrectJob(), "A")}";
                        }
                        else if (layer.layerTypeindex == 9)
                        {
                            rtb_layerhelper.Text += $"\n{GameHelper.GetJobClassDynamicLayerDescriptions(GameController.GetCurrectJob(), "B")}";
                        }
                    }
                    else if (layer.rootLayerType == LayerType.BaseLayer)
                    {
                        rtb_layerhelper.Text = TextHelper.ParseLayerHelperText(((LayerDisplay)typeof(BaseLayerType).GetField(Enum.GetName(typeof(BaseLayerType), layer.layerTypeindex)).GetCustomAttribute(typeof(LayerDisplay))).Description);
                    }
                    else if (layer.rootLayerType == LayerType.EffectLayer)
                    {
                        rtb_layerhelper.Text = TextHelper.ParseLayerHelperText(@"The effect layer displays effects over other layers, depending on which effects are enabled.");
                    }
                });
            }
            else
            {
                if (layer.rootLayerType == LayerType.DynamicLayer)
                {
                    rtb_layerhelper.Text = TextHelper.ParseLayerHelperText(((LayerDisplay)typeof(DynamicLayerType).GetField(Enum.GetName(typeof(DynamicLayerType), layer.layerTypeindex)).GetCustomAttribute(typeof(LayerDisplay))).Description);

                    if (layer.layerTypeindex == 8)
                    {
                        rtb_layerhelper.Text += $"\n{GameHelper.GetJobClassDynamicLayerDescriptions(GameController.GetCurrectJob(), "A")}";
                    }
                    else if (layer.layerTypeindex == 9)
                    {
                        rtb_layerhelper.Text += $"\n{GameHelper.GetJobClassDynamicLayerDescriptions(GameController.GetCurrectJob(), "B")}";
                    }
                }
                else if (layer.rootLayerType == LayerType.BaseLayer)
                {
                    rtb_layerhelper.Text = TextHelper.ParseLayerHelperText(((LayerDisplay)typeof(BaseLayerType).GetField(Enum.GetName(typeof(BaseLayerType), layer.layerTypeindex)).GetCustomAttribute(typeof(LayerDisplay))).Description);
                }
                else if (layer.rootLayerType == LayerType.EffectLayer)
                {
                    rtb_layerhelper.Text = TextHelper.ParseLayerHelperText(@"The effect layer displays effects over other layers, depending on which effects are enabled.");
                }
            }
        }

        private void OnEditButtonPressed(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            var obj = (MetroButton)sender;
            var parent = (Pn_LayerDisplay)obj.Parent;

            foreach (var layers in _layers)
            {
                if (layers == parent) continue;

                if (layers.editing)
                {
                    layers.editing = false;
                    layers.Invalidate();
                }
            }

            if (parent.editing)
            {
                //Save Layers
                btn_clearselection.Enabled = false;
                btn_reverseselection.Enabled = false;
                btn_undoselection.Enabled = false;
                btn_togglebleed.Enabled = false;
                cb_changemode.Enabled = false;
                parent.editing = false;
                currentlyEditing = null;

                var ms = MappingLayers.GetLayer(parent.ID);
                ms.deviceLeds.Clear();

                int i = 1;
                foreach (var led in currentKeySelection.OrderBy(kvp => kvp.Key))
                {
                    ms.deviceLeds.Add(i, led.Value);
                    i++;
                }

                ms.requestUpdate = true;
                MappingLayers.UpdateLayer(ms);

#if DEBUG
                Debug.WriteLine($"Saved {ms.deviceLeds.Count} leds for layer {ms.layerID}");
#endif

                SaveLayers();
                RevertButtons();
            }
            else
            {
                //Load Layer for editing
                RevertButtons();

                var selectedDeviceItem = cb_deviceselect.SelectedItem as ComboboxItem;
                if (selectedDeviceItem == null || selectedDeviceItem.Value is not Guid selectedDeviceId)
                {
                    return;
                }

                var selectedDevice = RGBController.GetLiveDevices().FirstOrDefault(d => d.Key == selectedDeviceId).Value;

                if (selectedDevice != null)
                {
                    if (currentlySelected != null)
                    {
                        currentlySelected.selected = false;
                        currentlySelected = null;

                        var virtualDevice = tlp_frame.Controls.OfType<VirtualDevice>().FirstOrDefault(vd => vd.Visible);
                        if (virtualDevice != null)
                        {
                            foreach (var key in virtualDevice._keybuttons)
                            {
                                if (!key.IsEditing && key.BorderCol != System.Drawing.Color.Black)
                                {
                                    key.BorderCol = System.Drawing.Color.Black;
                                    key.Invalidate();
                                }
                            }
                        }
                    }

                    btn_clearselection.Enabled = true;
                    btn_reverseselection.Enabled = true;
                    btn_undoselection.Enabled = true;
                    btn_togglebleed.Enabled = true;
                    cb_changemode.Enabled = true;
                    parent.editing = true;
                    currentlyEditing = parent;

                    currentKeySelection.Clear();
                    _currentSelectedKeys.Clear();

                    var ml = MappingLayers.GetLayer(parent.ID);
                    var _selection = new Dictionary<int, LedId>();

                    if (ml.allowBleed)
                    {
                        btn_togglebleed.Text = @"Bleed Enabled";
                        btn_togglebleed.BackColor = System.Drawing.Color.Lime;
                    }
                    else
                    {
                        btn_togglebleed.Text = @"Bleed Disabled";
                        btn_togglebleed.BackColor = System.Drawing.Color.Red;
                    }

                    ChangeLayerMode(ml);

                    if (ml.rootLayerType == LayerType.DynamicLayer)
                    {
                        rtb_layerhelper.Text = TextHelper.ParseLayerHelperText(((LayerDisplay)typeof(DynamicLayerType).GetField(Enum.GetName(typeof(DynamicLayerType), ml.layerTypeindex)).GetCustomAttribute(typeof(LayerDisplay))).Description);
                    }
                    else if (ml.rootLayerType == LayerType.BaseLayer)
                    {
                        rtb_layerhelper.Text = TextHelper.ParseLayerHelperText(((LayerDisplay)typeof(BaseLayerType).GetField(Enum.GetName(typeof(BaseLayerType), ml.layerTypeindex)).GetCustomAttribute(typeof(LayerDisplay))).Description);
                    }
                    else if (ml.rootLayerType == LayerType.EffectLayer)
                    {
                        rtb_layerhelper.Text = TextHelper.ParseLayerHelperText(@"The effect layer displays effects over other layers, depending on which effects are enabled.");
                    }

                    Debug.WriteLine($"Key Count: {ml.deviceLeds.Count}");

                    foreach (var led in ml.deviceLeds)
                    {
                        _selection.Add(led.Key, led.Value);
                    }

#if DEBUG
                    Debug.WriteLine($"Loaded {_selection.Count} leds for layer {ml.layerID}");
#endif

                    var virtualDeviceToShow = tlp_frame.Controls.OfType<VirtualDevice>().FirstOrDefault(vd => vd.Visible);
                    if (virtualDeviceToShow != null && _selection.Count > 0)
                    {
                        var i = 1;

                        if (_selection.First().Key == 1)
                        {
                            foreach (var selection in _selection)
                            {
                                foreach (var key in virtualDeviceToShow._keybuttons)
                                {
                                    if (key.KeyType == selection.Value)
                                    {
                                        key.RemoveCircle();

                                        //Add Key to holding dictionary and light button
                                        key.BackColor = (System.Drawing.Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(currentlyEditing.LayerType).Value;
                                        key.BorderCol = System.Drawing.Color.Red;
                                        key.AddCircle((i).ToString());

                                        _currentSelectedKeys.Add(key);
                                    }
                                }

                                i++;
                            }
                        }
                        else
                        {
                            i = 0;
                            foreach (var selection in _selection)
                            {
                                foreach (var key in virtualDeviceToShow._keybuttons)
                                {
                                    if (key.KeyType == selection.Value)
                                    {
                                        key.RemoveCircle();

                                        //Add Key to holding dictionary and light button
                                        key.BackColor = (System.Drawing.Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(currentlyEditing.LayerType).Value;
                                        key.BorderCol = System.Drawing.Color.Red;
                                        key.AddCircle((_selection.Count - i).ToString());

                                        _currentSelectedKeys.Add(key);
                                    }
                                }

                                i++;
                            }
                        }

                        currentKeySelection = _selection;
                    }
                }
            }

            parent.Invalidate();
            var thisbtn = (MetroButton)sender;
            this.ActiveControl = thisbtn.Parent;
        }

        private void OnCopyButtonPressed(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            var obj = (MetroButton)sender;
            var parent = (Pn_LayerDisplay)obj.Parent;

            if (selectedAddType != parent.LayerType)
                selectedAddType = parent.LayerType;

            var ms = MappingLayers.GetLayer(parent.ID);

            var newid = AddLayer(selectedAddType, ms.deviceGuid, ms.deviceType, 0, 2, true, true, false, ms.deviceLeds, ms.layerTypeindex, ms.allowBleed, ms.layerModes);

            foreach (Pn_LayerDisplay layer in flp_layers.Controls)
            {
                if (layer.ID == newid) continue;

                var _layer = MappingLayers.GetLayer(layer.ID);

                if (_layer.zindex == 1)
                {
                    flp_layers.Controls.SetChildIndex(layer, flp_layers.Controls.Count);
                }
                else if (_layer.zindex >= 2)
                {
                    _layer.zindex++;
                    MappingLayers.UpdateLayer(_layer);

                    layer.LeftText = $"{_layer.zindex}";
                    layer.Invalidate();
                }
            }

            SaveLayers();

            if (currentlyEditing == null)
            {
                this.ActiveControl = flp_layers.Controls[flp_layers.Controls.Count - 2];
            }
            else
            {
                var thisbtn = (MetroButton)sender;
                this.ActiveControl = thisbtn.Parent;
            }
        }

        private void OnDeleteButtonPressed(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            var obj = (MetroButton)sender;
            var parent = (Pn_LayerDisplay)obj.Parent;

            switch (MessageBox.Show(Fm_MainWindow.ActiveForm, @"Are you sure you want to delete this layer?", @"", MessageBoxButtons.OKCancel, MessageBoxIcon.Information))
            {
                case DialogResult.OK:

                    //Cleanup
                    var selectedDeviceItem = cb_deviceselect.SelectedItem as ComboboxItem;
                    if (selectedDeviceItem == null || !(selectedDeviceItem.Value is Guid selectedDeviceId))
                    {
                        return;
                    }

                    var selectedDevice = RGBController.GetLiveDevices().FirstOrDefault(d => d.Key == selectedDeviceId).Value;

                    if (selectedDevice != null)
                    {
                        if (currentlySelected != null)
                        {
                            currentlySelected.selected = false;
                            currentlySelected = null;
                        }

                        var virtualDevice = tlp_frame.Controls.OfType<VirtualDevice>().FirstOrDefault(vd => vd.Visible);
                        if (virtualDevice != null)
                        {
                            foreach (var key in virtualDevice._keybuttons)
                            {
                                if (!key.IsEditing && key.BorderCol != System.Drawing.Color.Black)
                                {
                                    key.BorderCol = System.Drawing.Color.Black;
                                    key.Invalidate();
                                }
                            }
                        }
                    }


                    if (currentlyEditing == parent)
                    {
                        parent.editing = false;
                        btn_clearselection.Enabled = false;
                        btn_reverseselection.Enabled = false;
                        btn_undoselection.Enabled = false;
                        btn_togglebleed.Enabled = false;
                        cb_changemode.Enabled = false;

                        RevertButtons();

                        currentlyEditing = null;
                    }

                    var targetid = parent.ID;
                    parent.editing = false;
                    flp_layers.Controls.Remove(parent);

                    // Reorder
                    foreach (Pn_LayerDisplay layer in flp_layers.Controls)
                    {
                        var _layer = MappingLayers.GetLayer(layer.ID);
                        var i = flp_layers.Controls.Count - flp_layers.Controls.GetChildIndex(layer);

                        _layer.zindex = i;

                        MappingLayers.UpdateLayer(_layer);
                        layer.LeftText = i.ToString();

                        layer.Invalidate();
                    }

                    //Remove Element
                    _layers.Remove(parent);
                    RGBController.RemoveLayerGroup(targetid);
                    MappingLayers.RemoveLayer(targetid);

                    rtb_layerhelper.Text = @"";

                    SaveLayers();

                    parent.cb_selector.SelectedIndexChanged -= new EventHandler(OnSelectedIndexChanged);
                    parent.chk_enabled.CheckedChanged -= new EventHandler(OnCheckChanged);
                    parent.btn_edit.Click -= new EventHandler(OnEditButtonPressed);
                    parent.btn_delete.Click -= new EventHandler(OnDeleteButtonPressed);
                    parent.btn_copy.Click -= new EventHandler(OnCopyButtonPressed);

                    VisualiseLayers();
                    parent.Dispose();
                    break;
                case DialogResult.Cancel:
                    break;
            }

            var thisbtn = (MetroButton)sender;
            this.ActiveControl = thisbtn.Parent;
        }

        private void OnKeyCapPressed(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            var keycap = (KeyButton)sender;

            if (currentlyEditing != null)
            {
                if (currentKeySelection.ContainsValue(keycap.KeyType))
                {
                    //Remove key from holding dictionary and revert button
                    keycap.BackColor = System.Drawing.Color.DarkGray;
                    keycap.BorderCol = System.Drawing.Color.Black;
                    keycap.RemoveCircle();

                    foreach (var item in currentKeySelection.Where(kvp => kvp.Value == keycap.KeyType).ToList())
                    {
                        currentKeySelection.Remove(item.Key);
                        _currentSelectedKeys.Remove(keycap);
                    }

                    if (currentKeySelection.Count > 0)
                    {
                        var _selection = new Dictionary<int, LedId>();
                        var i = 1;

                        if (currentKeySelection.First().Key == 1)
                        {
                            foreach (var selection in currentKeySelection)
                            {
                                _selection.Add(i, selection.Value);
                                foreach (var key in _currentSelectedKeys)
                                {
                                    if (key.KeyType == selection.Value)
                                    {
                                        key.RemoveCircle();
                                        key.AddCircle((i).ToString());
                                    }
                                }

                                i++;
                            }
                        }
                        else
                        {
                            i = 0;
                            foreach (var selection in currentKeySelection)
                            {
                                _selection.Add(currentKeySelection.Count - i, selection.Value);

                                var keycapToUpdate = _currentSelectedKeys.FirstOrDefault(k => k.KeyType == selection.Value);
                                if (keycapToUpdate != null)
                                {
                                    keycapToUpdate.RemoveCircle();
                                    keycapToUpdate.AddCircle((currentKeySelection.Count - i).ToString());
                                }

                                i++;
                            }
                        }

                        currentKeySelection = _selection;
                    }
                }
                else
                {
                    //Add Key to holding dictionary and light button
                    keycap.BackColor = (System.Drawing.Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(currentlyEditing.LayerType).Value;
                    keycap.BorderCol = System.Drawing.Color.Red;
                    keycap.AddCircle((currentKeySelection.Count + 1).ToString());

                    currentKeySelection.Add(currentKeySelection.Count + 1, keycap.KeyType);
                    _currentSelectedKeys.Add(keycap);
                }
            }
        }

        private void btn_addlayer_Click(object sender, EventArgs e)
        {
            if (!init) return;

            if (selectedAddType != LayerType.DynamicLayer)
                selectedAddType = LayerType.DynamicLayer;

            var selectedDeviceGuid = GetActiveDevice()?.Value as Guid?;
            var selectedDeviceType = RGBController.GetLiveDevices().FirstOrDefault(d => d.Key == selectedDeviceGuid).Value?.DeviceInfo.DeviceType;

            if (selectedDeviceGuid == null || selectedDeviceType == null)
            {
                MessageBox.Show("Please select a valid device.");
                return;
            }

            var newid = AddLayer(selectedAddType, selectedDeviceGuid.Value, selectedDeviceType.Value, 0, 2, true, true);

            foreach (Pn_LayerDisplay layer in flp_layers.Controls)
            {
                if (layer.ID == newid) continue;

                var _layer = MappingLayers.GetLayer(layer.ID);

                if (_layer.zindex == 1)
                {
                    flp_layers.Controls.SetChildIndex(layer, flp_layers.Controls.Count);
                }
                else if (_layer.zindex >= 2)
                {
                    _layer.zindex++;
                    MappingLayers.UpdateLayer(_layer);

                    layer.LeftText = $"{_layer.zindex}";
                    layer.Invalidate();
                }

#if DEBUG
                Debug.WriteLine(@"NEW Layer: " + layer.ID + @". Layer ID: " + _layer.layerID + @". zindex: " + _layer.zindex + @". Type: " + _layer.rootLayerType);
#endif
            }

            SaveLayers();
            var thisbtn = (MetroButton)sender;
            this.ActiveControl = thisbtn.Parent;
        }

        private void cb_addlayer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!init) return;

            var cmb = (MetroComboBox)sender;

            if (cmb.Items.Count > 0)
            {
                var value = (ComboboxItem)cmb.SelectedItem;

                if (value != null)
                {
                    selectedAddType = (LayerType)Enum.Parse(typeof(LayerType), value.Value.ToString());
                }
            }
        }

        private void cb_deviceselect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!init) return;

            if (cb_deviceselect.SelectedItem is ComboboxItem selectedItem)
            {
                if (selectedItem.Value is Guid selectedDeviceId)
                {
                    var _selectedDevice = RGBController.GetLiveDevices().FirstOrDefault(d => d.Key == selectedDeviceId).Value;

                    if (_selectedDevice != null)
                    {
                        var selectedDeviceType = _selectedDevice.DeviceInfo.DeviceType;

                        if (selectedDeviceType == selectedDevice) return;

                        selectedDevice = selectedDeviceType;
                        ChangeDeviceType();
                    }
                }
            }
            else
            {
                // Handle case where no item is selected or the item is not a ComboboxItem
                MessageBox.Show("Please select a valid device.");
            }
        }

        private void btn_preview_Click(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            var btn = (MetroButton)sender;

            if (MappingLayers.IsPreview())
            {
                btn.BackColor = SystemColors.Control;
                MappingLayers.SetPreview(false);

                RevertButtons();
                VisualiseLayers();
            }
            else
            {
                btn.BackColor = System.Drawing.Color.LimeGreen;
                MappingLayers.SetPreview(true);

            }

            var thisbtn = (MetroButton)sender;
            this.ActiveControl = thisbtn.Parent;
        }

        private void btn_clearselection_Click(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            var parent = currentlyEditing;

            var ms = MappingLayers.GetLayer(parent.ID);
            ms.deviceLeds.Clear();

            foreach (var led in currentKeySelection)
            {
                if (ms.deviceLeds.ContainsKey(led.Key))
                    ms.deviceLeds.Remove(led.Key);
            }

            ms.requestUpdate = true;
            MappingLayers.UpdateLayer(ms);

            SaveLayers();
            RevertButtons();

            btn_clearselection.Enabled = false;
            btn_reverseselection.Enabled = false;
            btn_undoselection.Enabled = false;
            btn_togglebleed.Enabled = false;
            cb_changemode.Enabled = false;
            currentlyEditing.editing = false;
            currentlyEditing = null;
            parent.Update();

#if DEBUG
            Debug.WriteLine($"Clearing Layer {parent.ID}");
#endif

            var thisbtn = (MetroButton)sender;
            this.ActiveControl = thisbtn.Parent;
        }

        private void btn_reverseselection_Click(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            if (currentKeySelection.Count > 0)
            {
                var _selection = new Dictionary<int, LedId>();
                var i = 0;

                if (currentKeySelection.First().Key == 1)
                {
                    //Forward Reverse
                    foreach (var selection in currentKeySelection)
                    {
                        _selection.Add(currentKeySelection.Count - i, selection.Value);

                        var keycap = _currentSelectedKeys.Where(value => value.KeyType == selection.Value).FirstOrDefault();
                        keycap.RemoveCircle();
                        keycap.AddCircle((currentKeySelection.Count - i).ToString());
                        i++;
                    }
                }
                else
                {
                    //Backwards Reverse
                    i = 1;
                    foreach (var selection in currentKeySelection)
                    {
                        _selection.Add(i, selection.Value);

                        var keycap = _currentSelectedKeys.Where(value => value.KeyType == selection.Value).FirstOrDefault();
                        keycap.RemoveCircle();
                        keycap.AddCircle((i).ToString());
                        i++;
                    }
                }

                currentKeySelection = _selection;
            }

#if DEBUG
            Debug.WriteLine($"Reversing Layer {currentlyEditing.ID}");
#endif

            var thisbtn = (MetroButton)sender;
            this.ActiveControl = thisbtn.Parent;
        }

        private void btn_undoselection_Click(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            if (currentKeySelection.Count > 0 && currentlyEditing != null)
            {
                RevertButtons();

                currentKeySelection.Clear();
                _currentSelectedKeys.Clear();

                var ml = MappingLayers.GetLayer(currentlyEditing.ID);
                var _selection = new Dictionary<int, LedId>();

                foreach (var led in ml.deviceLeds)
                {
                    _selection.Add(led.Key, led.Value);
                }

#if DEBUG
                Debug.WriteLine($"Reloaded {_selection.Count} leds for layer {ml.layerID}");
#endif

                var virtualDeviceToShow = tlp_frame.Controls.OfType<VirtualDevice>().FirstOrDefault(vd => vd.Visible);
                if (virtualDeviceToShow != null)
                {
                    var i = 1;

                    if (_selection.First().Key == 1)
                    {
                        foreach (var selection in _selection)
                        {
                            foreach (var key in virtualDeviceToShow._keybuttons)
                            {
                                if (key.KeyType == selection.Value)
                                {
                                    key.RemoveCircle();

                                    //Add Key to holding dictionary and light button
                                    key.BackColor = (System.Drawing.Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(currentlyEditing.LayerType).Value;
                                    key.BorderCol = System.Drawing.Color.Red;
                                    key.AddCircle((i).ToString());

                                    _currentSelectedKeys.Add(key);
                                }
                            }

                            i++;
                        }
                    }
                    else
                    {
                        i = 0;
                        foreach (var selection in _selection)
                        {
                            foreach (var key in virtualDeviceToShow._keybuttons)
                            {
                                if (key.KeyType == selection.Value)
                                {
                                    key.RemoveCircle();

                                    //Add Key to holding dictionary and light button
                                    key.BackColor = (System.Drawing.Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(currentlyEditing.LayerType).Value;
                                    key.BorderCol = System.Drawing.Color.Red;
                                    key.AddCircle((_selection.Count - i).ToString());

                                    _currentSelectedKeys.Add(key);
                                }
                            }

                            i++;
                        }
                    }

                    currentKeySelection = _selection;
                }

                currentlyEditing.Update();
            }

            var thisbtn = (MetroButton)sender;
            this.ActiveControl = thisbtn.Parent;
        }

        private void btn_import_Click(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            if (MappingLayers.ImportMappings())
            {
                MappingLayers.SaveMappings();
                ChangeDeviceType();
            }
        }

        private void btn_export_Click(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            MappingLayers.ExportMappings();
        }

        private void btn_togglebleed_Click(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            if (currentKeySelection.Count > 0 && currentlyEditing != null)
            {
                var parent = currentlyEditing;
                var thisbtn = (MetroButton)sender;

                var ms = MappingLayers.GetLayer(parent.ID);

                if (ms.allowBleed)
                {
                    //Disable Bleed
                    thisbtn.Text = @"Bleed Disabled";
                    thisbtn.BackColor = System.Drawing.Color.Red;
                    ms.allowBleed = false;
                }
                else
                {
                    //Allow Bleed
                    thisbtn.Text = @"Bleed Enabled";
                    thisbtn.BackColor = System.Drawing.Color.Lime;
                    ms.allowBleed = true;
                }

                MappingLayers.UpdateLayer(ms);

                SaveLayers();

#if DEBUG
                Debug.WriteLine($"Toggle bleeding on Layer {parent.ID}: {ms.allowBleed}");
#endif

                this.ActiveControl = thisbtn.Parent;
            }
        }

        private void cb_changemode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            var cmb = (MetroComboBox)sender;

            if (cmb.Items.Count > 0)
            {
                if (currentKeySelection.Count > 0 && currentlyEditing != null)
                {
                    var parent = currentlyEditing;

                    var value = (ComboboxItem)cmb.SelectedItem;

                    if (value != null)
                    {
                        var ms = MappingLayers.GetLayer(parent.ID);
                        ms.layerModes = (LayerModes)Enum.Parse(typeof(LayerModes), value.Value.ToString());

                        MappingLayers.UpdateLayer(ms);
                        SaveLayers();
                    }

                    this.ActiveControl = cmb.Parent;
                }
            }
        }

        private void ChangeLayerMode(Layer layer)
        {
            Type type = typeof(BaseLayerType);

            switch (layer.rootLayerType)
            {
                case LayerType.BaseLayer:
                    type = typeof(BaseLayerType);
                    break;
                case LayerType.DynamicLayer:
                    type = typeof(DynamicLayerType);
                    break;
                case LayerType.EffectLayer:
                    type = typeof(EffectLayerType);
                    break;
            }

            var layerTypesSupported = ((LayerDisplay)type.GetField(Enum.GetName(type, layer.layerTypeindex)).GetCustomAttributes(false).FirstOrDefault(x => x is LayerDisplay)).LayerTypeCompatibility;

            cb_changemode.Items.Clear();

            if (layerTypesSupported != null && layerTypesSupported.Length > 0)
            {
                foreach (var mode in layerTypesSupported)
                {
                    var item = new ComboboxItem { Value = mode, Text = EnumExtensions.GetAttribute<DisplayAttribute>(mode).Name };
                    cb_changemode.Items.Add(item);

                    if (mode == layer.layerModes)
                    {
                        cb_changemode.SelectedItem = item;
                    }
                }

                if (cb_changemode.SelectedIndex < 0)
                    cb_changemode.SelectedIndex = 0;
            }
            else
            {
                var item = new ComboboxItem { Value = LayerModes.None, Text = EnumExtensions.GetAttribute<DisplayAttribute>(LayerModes.None).Name };
                cb_changemode.Items.Add(item);
                cb_changemode.SelectedItem = item;
                cb_changemode.Enabled = false;
            }
        }

        private void ResizeLayerHelpText(RichTextBox rtb)
        {
            // Set a maximum font size
            int maxFontSize = 10;
            int minFontSize = 8;

            // Get the font size for the current text
            float fontSize = rtb.Font.Size;

            // Get the current text
            string text = rtb.Text;

            // Suspend layout updates
            rtb.SuspendLayout();

            // Create a new Graphics object
            using (var g = rtb.CreateGraphics())
            {
                // Measure the size of the current text, taking into account the padding value
                SizeF textSize = g.MeasureString(text, rtb.Font, (int)(rtb.Width - rtb.Padding.Left - rtb.Padding.Right));

                // Check if the text fits within the RichTextBox
                if (textSize.Height > rtb.Height - rtb.Padding.Top - rtb.Padding.Bottom)
                {
                    // Reduce the font size until the text fits within the RichTextBox
                    while (textSize.Height > rtb.Height - rtb.Padding.Top - rtb.Padding.Bottom)
                    {
                        // Reduce the font size by 1
                        fontSize--;

                        // Check if the font size exceeds the minimum
                        if (fontSize <= minFontSize)
                        {
                            break;
                        }

                        // Measure the size of the text with the new font size
                        textSize = g.MeasureString(text, new Font(rtb.Font.FontFamily, fontSize), (int)(rtb.Width - rtb.Padding.Left - rtb.Padding.Right));
                    }

                    // Update the font size for the RichTextBox
                    rtb.Font = new Font(rtb.Font.FontFamily, fontSize);
                }
                else if (textSize.Height <= rtb.Height - rtb.Padding.Top - rtb.Padding.Bottom && fontSize < maxFontSize)
                {
                    // Increase the font size until the text fits within the RichTextBox
                    while (textSize.Height <= rtb.Height - rtb.Padding.Top - rtb.Padding.Bottom && fontSize < maxFontSize)
                    {
                        // Increase the font size by 1
                        fontSize++;

                        // Measure the size of the text with the new font size
                        textSize = g.MeasureString(text, new Font(rtb.Font.FontFamily, fontSize), (int)(rtb.Width - rtb.Padding.Left - rtb.Padding.Right));
                    }

                    // Update the font size for the RichTextBox
                    rtb.Font = new Font(rtb.Font.FontFamily, fontSize);
                }
            }

            // Resume layout updates
            rtb.ResumeLayout();
        }

        private void rtb_layerhelper_TextChanged(object sender, EventArgs e)
        {
            ResizeLayerHelpText((RichTextBox)sender);
        }

        private void Kh_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey)
            {
                //Shift key pressed
                _layers.ForEach(x => { x.SuspendLayout(); });

                foreach (var pgb in _layers)
                {
                    if (pgb.btn_delete.Visible == true)
                    {
                        pgb.btn_delete.Visible = false;
                        pgb.btn_copy.Visible = true;
                    }
                }

                _layers.ForEach(x => { x.ResumeLayout(); });
            }
        }

        private void Kh_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey)
            {
                _layers.ForEach(x => { x.SuspendLayout(); });

                //Shift key released
                foreach (var pgb in _layers)
                {
                    if (pgb.btn_copy.Visible == true)
                    {
                        pgb.btn_copy.Visible = false;
                        pgb.btn_delete.Visible = true;
                    }
                }

                _layers.ForEach(x => { x.ResumeLayout(); });
            }
        }

        private void gameJobChanged()
        {
            foreach (var layer in MappingLayers.GetLayers())
            {
                ResetLayerText(layer.Value);
            }
        }

        public class ComboboxItem
        {
            public object Value { get; set; }
            public string Text { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }
    }
}
