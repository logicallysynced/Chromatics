using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions;
using Chromatics.Helpers;
using Chromatics.Layers;
using Chromatics.Models;
using MetroFramework;
using MetroFramework.Components;
using MetroFramework.Controls;
using RGB.NET.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Chromatics.Models.VirtualDevice;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace Chromatics.Forms
{
    public partial class Uc_Mappings : UserControl
    {
        public MetroTabControl TabManager { get; set; }

        private List<Pn_LayerDisplay> _layers = new List<Pn_LayerDisplay>();
        private List<KeyButton> _currentSelectedKeys = new List<KeyButton>();
        private Dictionary<int, LedId> currentKeySelection = new Dictionary<int,LedId>();
        private Dictionary<RGBDeviceType, VirtualDevice> _virtualDevices;
        private HashSet<int> _visibleLayers = new HashSet<int>();
        private int _layerPad = 5;
        private MetroToolTip tt_mappings;
        private LayerType selectedAddType;
        private RGBDeviceType selectedDevice;
        private Pn_LayerDisplay currentlyEditing;
        private Pn_LayerDisplay currentlySelected;
        private bool init;
        private bool IsAddingLayer;

        public Uc_Mappings()
        {
            InitializeComponent();
            tlp_base.Size = this.Size;

            this.flp_layers.DragEnter += new DragEventHandler(flp_layers_DragEnter);
            this.flp_layers.DragDrop += new DragEventHandler(flp_layers_DragDrop);

            _virtualDevices = new Dictionary<RGBDeviceType, VirtualDevice>()
            {
                { RGBDeviceType.Keyboard, new Uc_VirtualKeyboard() },
                { RGBDeviceType.Mouse, new Uc_VirtualMouse() },
                { RGBDeviceType.Headset, new Uc_VirtualHeadset() },
                { RGBDeviceType.Mousepad, new Uc_VirtualMousePad() },
                { RGBDeviceType.LedStripe, new Uc_VirtualLedStrip() },
                { RGBDeviceType.LedMatrix, new Uc_VirtualLedMatrix() },
                { RGBDeviceType.Mainboard, new Uc_VirtualMainboard() },
                { RGBDeviceType.GraphicsCard, new Uc_VirtualGraphicsCard() },
                { RGBDeviceType.DRAM, new Uc_VirtualDRAM() },
                { RGBDeviceType.HeadsetStand, new Uc_VirtualHeadsetStand() },
                { RGBDeviceType.Keypad, new Uc_VirtualKeypad() },
                { RGBDeviceType.Fan, new Uc_VirtualFan() },
                { RGBDeviceType.Speaker, new Uc_VirtualSpeaker() },
                { RGBDeviceType.Cooler, new Uc_VirtualCooler() },
                { RGBDeviceType.LedController, new Uc_VirtualLedController() }
            };

            // Add each virtual device control to the form
            foreach (var virtualDevice in _virtualDevices.Values)
            {
                virtualDevice.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                virtualDevice.Dock = DockStyle.Top;
                virtualDevice.MinimumSize = new Size(1200, 300);
                virtualDevice.AutoSize = false;
                tlp_frame.Controls.Add(virtualDevice, 0, 0);

                virtualDevice._OnKeycapPressed += new EventHandler(OnKeyCapPressed);
            }

            // Hide all virtual device controls initially
            HideAllVirtualDevices();
        }

        void flp_layers_DragDrop(object sender, DragEventArgs e)
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
                    else if (index >= _layers.Count-1)
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

        void flp_layers_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
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

            //Enumerate DeviceTypes into Device Combobox
            foreach (Enum lt in Enum.GetValues(typeof(RGBDeviceType)))
            {
                if (!_virtualDevices.ContainsKey((RGBDeviceType)lt)) continue;

                cb_deviceselect.Items.Add(lt);
            }

            cb_deviceselect.SelectedIndex = 0;
            selectedDevice = RGBDeviceType.Keyboard;

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
            if (MappingLayers.LoadMappings())
            {
                var layercount = MappingLayers.CountLayers();
                Logger.WriteConsole(LoggerTypes.System, $"Loaded {layercount} layers from layers.chromatics3");

                ChangeDeviceType();
                
            }
            else
            {
                CreateDefaults();
                SaveLayers(true);
            }
                        
            //Add tooltips
            tt_mappings.SetToolTip(this.cb_addlayer, @"Add New Layer of selected type");
            tt_mappings.SetToolTip(this.cb_deviceselect, @"Change to another device");
            tt_mappings.SetToolTip(this.btn_preview, @"Display layers on physical devices");
            tt_mappings.SetToolTip(this.btn_clearselection, @"Clear all keys on layer");
            tt_mappings.SetToolTip(this.btn_reverseselection, @"Reverse keys on layer");
            tt_mappings.SetToolTip(this.btn_undoselection, @"Undo key selection on layer");
            tt_mappings.SetToolTip(this.btn_togglebleed, @"For layers which have a negative colour, allow lower layers to bleed through instead of showing the negative colour.");
            tt_mappings.SetToolTip(this.cb_changemode, @"Change the layer mode." + Environment.NewLine + @"Interpolate: Shows the layer as a bar on your device." + Environment.NewLine + @"Fade: Fades the colour of the RGB keys.");

            //Handle Events
            this.TabManager.Selecting += new TabControlCancelEventHandler(mT_TabManager_Selecting);

            //Set init to true
            init = true;
        }

        private void SaveLayers(bool bypass = false)
        {
            if (init || bypass)
            {
                MappingLayers.SaveMappings();
            }
        }

        private int AddLayer(LayerType layertype, RGBDeviceType devicetype, int id = 0, int index = 0, bool initialize = false, bool bypass = false, bool enabled = false, Dictionary<int, LedId> leds = null)
        {
            IsAddingLayer = true;
            var s = new Size(flp_layers.Width - _layerPad, 50);

            var pgb = new Pn_LayerDisplay
            {
                Padding = new Padding(_layerPad, _layerPad, _layerPad, _layerPad),
                LeftText = (_layers.Count + 1).ToString(),
                LayerType = layertype,
                Size = s,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Dock = DockStyle.Top
            };
                       

            tt_mappings.SetToolTip(pgb.chk_enabled, "Enable/Disable Layer");
            tt_mappings.SetToolTip(pgb.cb_selector, "Select the type of Layer");
            tt_mappings.SetToolTip(pgb.btn_edit, "Toggle Edit mode for this Layer");
            tt_mappings.SetToolTip(pgb.btn_delete, "Remove this Layer");

            pgb.GotFocus += new EventHandler(OnLayerPressed);
            pgb.cb_selector.SelectedIndexChanged += new EventHandler(OnSelectedIndexChanged);
            pgb.chk_enabled.CheckedChanged += new EventHandler(OnCheckChanged);
            pgb.btn_edit.Click += new EventHandler(OnEditButtonPressed);
            pgb.btn_delete.Click += new EventHandler(OnDeleteButtonPressed);
                       

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

                pgb.ID = MappingLayers.AddLayer(MappingLayers.CountLayers(), layertype, devicetype, 0, _index, enabled, leds);
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
            //Create Default Layers
            Logger.WriteConsole(LoggerTypes.System, @"No layer file found. Creating default layers..");

            foreach (Enum lt in Enum.GetValues(typeof(RGBDeviceType)))
            {
                var i = 1;

                if ((RGBDeviceType)lt == RGBDeviceType.None || (RGBDeviceType)lt == RGBDeviceType.Unknown || (RGBDeviceType)lt == RGBDeviceType.All)
                    continue;

                AddLayer(LayerType.BaseLayer, (RGBDeviceType)lt, 0, i, true, false, true, LedKeyHelper.GetAllKeysForDevice((RGBDeviceType)lt));
                i++;

                if ((RGBDeviceType)lt == RGBDeviceType.Keyboard)
                {
                    AddLayer(LayerType.DynamicLayer, (RGBDeviceType)lt, 0, i, true, false, true);
                    i++;
                }

                AddLayer(LayerType.EffectLayer, (RGBDeviceType)lt, 0, i, true, false, true, LedKeyHelper.GetAllKeysForDevice((RGBDeviceType)lt));
            }

            ChangeDeviceType();
            SaveLayers();
        }

        

        private void ChangeDeviceType()
        {
            if (_virtualDevices.Count <= 0 || !_virtualDevices.ContainsKey(selectedDevice))
                return;

            // Hide all virtual device controls and show the selected one
            HideAllVirtualDevices();
            _virtualDevices[selectedDevice].Visible = true;

            // Suspend layout while making multiple changes
            tlp_frame.SuspendLayout();
            flp_layers.SuspendLayout();

            Debug.WriteLine($"Changing device type to {selectedDevice}.");

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

                layer.Dispose();
            }

            tt_mappings.RemoveAll();
            tt_mappings.SetToolTip(this.cb_addlayer, "Add New Layer of selected type");
            tt_mappings.SetToolTip(this.cb_deviceselect, "Change to another device");

            flp_layers.Controls.Clear();
            _layers.Clear();

            // Add layers for new device types
            var _NewLayers = MappingLayers.GetLayers().Where(r => r.Value.deviceType.Equals(selectedDevice));

            foreach (var layers in _NewLayers.OrderByDescending(r => r.Value.zindex))
            {
                AddLayer(layers.Value.rootLayerType, selectedDevice, layers.Key, layers.Value.zindex, false);
            }
                       

            // Resume layout
            tlp_frame.ResumeLayout();
            flp_layers.ResumeLayout();
    
            VisualiseLayers();
        }

        private void HideAllVirtualDevices()
        {
            foreach (var virtualDevice in _virtualDevices.Values)
            {
                virtualDevice.Visible = false;
            }
        }       

        private void VisualiseLayers(bool requestUpdate = true)
        {
            if (_virtualDevices.Count <= 0) return;

            var layers = MappingLayers.GetLayers().Where(x => x.Value.deviceType == selectedDevice).OrderBy(x => x.Value.zindex);
            
            foreach (var layer in layers)
            {
                var allKeyButtons = _virtualDevices.Values.SelectMany(x => x._keybuttons).ToList();

                foreach (var virtualDevice in _virtualDevices.Values)
                {
                    virtualDevice.VisualiseLayers(layers, allKeyButtons);
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

            VisualiseLayers(false);

            currentKeySelection.Clear();
            _currentSelectedKeys.Clear();
        }

        private void OnResize(object sender, EventArgs e)
        {
            foreach(var layer in _layers)
            {
                layer.Size = new Size(flp_layers.Width - _layerPad, 50);
                layer.Update();
            }
        }

        private void mT_TabManager_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (IsAddingLayer) return;

            var current = (sender as TabControl).SelectedTab.ToString();
            
            if (current == "tP_mappings")
            {
                if (MappingLayers.IsPreview())
                {
                    btn_preview.BackColor = System.Drawing.Color.LimeGreen;
                    MappingLayers.SetPreview(true);
                }
                else
                {
                    btn_preview.BackColor = SystemColors.Control;
                    MappingLayers.SetPreview(false);
                }
            }
            else
            {
                btn_preview.BackColor = SystemColors.Control;
                MappingLayers.SetPreview(false);
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
            layer.layerTypeindex = selectedindex;
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
            MappingLayers.UpdateLayer(layer);
            SaveLayers();
            VisualiseLayers(false);
        }

        private void OnLayerPressed(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;
            if (currentlyEditing != null) return;

            var obj = (Pn_LayerDisplay)sender;
            
            if (_virtualDevices.Count > 0 && _virtualDevices.ContainsKey(selectedDevice))
            {
                if (currentlySelected != null)
                {
                    currentlySelected.selected = false;
                    currentlySelected = null;

                    foreach (var key in _virtualDevices[selectedDevice]._keybuttons)
                    {
                        if (!key.IsEditing && key.BorderCol != System.Drawing.Color.Black)
                        {
                            key.BorderCol = System.Drawing.Color.Black;
                            key.Invalidate();
                        }
                    }
                }                               

                var layer = MappingLayers.GetLayer(obj.ID);

                foreach (var selection in layer.deviceLeds)
                {
                    foreach (var key in _virtualDevices[selectedDevice]._keybuttons)
                    {
                        if (key.KeyType == selection.Value && !key.IsEditing)
                        {
                            key.BorderCol = System.Drawing.Color.SandyBrown; //(System.Drawing.Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(obj.LayerType).Value;
                            key.Invalidate();
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

                var item = new ComboboxItem { Value = LayerModes.None, Text = EnumExtensions.GetAttribute<DisplayAttribute>(LayerModes.None).Name };

                if (layer.rootLayerType == LayerType.BaseLayer || layer.rootLayerType == LayerType.EffectLayer)
                {
                    cb_changemode.Items.Add(item);
                    cb_changemode.SelectedItem = item;
                }
                else
                {
                    foreach (ComboboxItem cb in cb_changemode.Items)
                    {
                        if ((LayerModes)cb.Value == LayerModes.None)
                        {
                            cb_changemode.Items.Remove(cb);
                        }

                        if ((LayerModes)cb.Value == layer.layerModes)
                        {
                            cb_changemode.SelectedItem = cb;
                        }
                    }
                }

                

                obj.selected = true;
                currentlySelected = obj;
                obj.Invalidate();
            }
        }

        private void OnEditButtonPressed(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;

            var obj = (MetroButton)sender;
            var parent = (Pn_LayerDisplay)obj.Parent;

            foreach(var layers in _layers)
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

                foreach (var led in currentKeySelection)
                {
                    ms.deviceLeds.Add(led.Key, led.Value);
                }

                ms.requestUpdate = true;
                MappingLayers.UpdateLayer(ms);

                Debug.WriteLine($"Saved {ms.deviceLeds.Count} leds for layer {ms.layerID}");

                SaveLayers();
                RevertButtons();
                                
            }
            else
            {
                //Load Layer for editing
                RevertButtons();

                if (currentlySelected != null && (_virtualDevices.Count > 0 && _virtualDevices.ContainsKey(selectedDevice)))
                {
                    currentlySelected.selected = false;
                    currentlySelected = null;

                    foreach (var key in _virtualDevices[selectedDevice]._keybuttons)
                    {
                        if (!key.IsEditing && key.BorderCol != System.Drawing.Color.Black)
                        {
                            key.BorderCol = System.Drawing.Color.Black;
                            key.Invalidate();
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

                foreach (ComboboxItem item in cb_changemode.Items)
                {
                    if ((LayerModes)item.Value == LayerModes.None)
                    {
                        cb_changemode.Items.Remove(item);
                    }

                    if ((LayerModes)item.Value == ml.layerModes)
                    {
                        cb_changemode.SelectedItem = item;
                    }
                }

                foreach (var led in ml.deviceLeds)
                {
                    _selection.Add(led.Key, led.Value);
                }

                Debug.WriteLine($"Loaded {_selection.Count} leds for layer {ml.layerID}");

                if (_selection.Count > 0 && (_virtualDevices.Count > 0 && _virtualDevices.ContainsKey(selectedDevice)))
                {
                    var i = 1;

                    if (_selection.First().Key == 1)
                    {
                        foreach (var selection in _selection)
                        {
                            foreach (var key in _virtualDevices[selectedDevice]._keybuttons)
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

                            foreach (var key in _virtualDevices[selectedDevice]._keybuttons)
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
            
            parent.Invalidate();
            var thisbtn = (MetroButton)sender;
            this.ActiveControl = thisbtn.Parent;
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
                    if (currentlySelected != null && (_virtualDevices.Count > 0 && _virtualDevices.ContainsKey(selectedDevice)))
                    {
                        currentlySelected.selected = false;
                        currentlySelected = null;

                        foreach (var key in _virtualDevices[selectedDevice]._keybuttons)
                        {
                            if (!key.IsEditing && key.BorderCol != System.Drawing.Color.Black)
                            {
                                key.BorderCol = System.Drawing.Color.Black;
                                key.Invalidate();
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

                    
                    SaveLayers();

                    parent.cb_selector.SelectedIndexChanged -= new EventHandler(OnSelectedIndexChanged);
                    parent.chk_enabled.CheckedChanged -= new EventHandler(OnCheckChanged);
                    parent.btn_edit.Click -= new EventHandler(OnEditButtonPressed);
                    parent.btn_delete.Click -= new EventHandler(OnDeleteButtonPressed);

                    VisualiseLayers(false);
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

                                foreach (var key in _currentSelectedKeys)
                                {
                                    if (key.KeyType == selection.Value)
                                    {
                                        key.RemoveCircle();
                                        key.AddCircle((currentKeySelection.Count - i).ToString());
                                    }
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

            var newid = AddLayer(selectedAddType, selectedDevice, 0, 2, true, true);

            foreach(Pn_LayerDisplay layer in flp_layers.Controls)
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

                Debug.WriteLine(@"NEW Layer: " + layer.ID + @". Layer ID: " + _layer.layerID + @". zindex: " + _layer.zindex + @". Type: " + _layer.rootLayerType);
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

            var cmb = (MetroComboBox)sender;

            if (cmb.Items.Count > 0)
            {
                var value = cmb.SelectedItem.ToString();

                if (value != null)
                {
                    var _devicetype = (RGBDeviceType)Enum.Parse(typeof(RGBDeviceType), value);

                    if (_devicetype == selectedDevice) return;

                    selectedDevice = _devicetype;
                    ChangeDeviceType();
                }
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

            Debug.WriteLine($"Clearing Layer {parent.ID}");
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

            Debug.WriteLine($"Reversing Layer {currentlyEditing.ID}");
            var thisbtn = (MetroButton)sender;
            this.ActiveControl = thisbtn.Parent;
        }

        private void btn_undoselection_Click(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;
            if (_virtualDevices.Count <= 0) return;
            
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

                Debug.WriteLine($"Reloaded {_selection.Count} leds for layer {ml.layerID}");

                if (_selection.Count > 0 && (_virtualDevices.Count > 0 && _virtualDevices.ContainsKey(selectedDevice)))
                {
                    var i = 1;

                    if (_selection.First().Key == 1)
                    {
                        foreach (var selection in _selection)
                        {
                            foreach (var key in _virtualDevices[selectedDevice]._keybuttons)
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

                            foreach (var key in _virtualDevices[selectedDevice]._keybuttons)
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
            if (_virtualDevices.Count <= 0) return;
            
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


                Debug.WriteLine($"Toggle bleeding on Layer {parent.ID}: {ms.allowBleed}");
                
                this.ActiveControl = thisbtn.Parent;
            }
        }

        private void cb_changemode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!init) return;
            if (IsAddingLayer) return;
            if (_virtualDevices.Count <= 0) return;
            
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
    }
}
