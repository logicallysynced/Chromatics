using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Plasmoid.Extensions;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.ComponentModel;
using Chromatics.Enums;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Chromatics.Helpers;
using MetroFramework.Controls;
using Chromatics.Extensions;

namespace Chromatics.Forms
{
    public class Pn_LayerDisplay : Panel, IDisposable
    {
        private MetroComboBox _cb_selector;
        private CheckBox _chk_enabled;
        private MetroButton _btn_edit;
        private MetroButton _btn_delete;
        private MetroButton _btn_copy;
        private bool _editing;

        public int ID { get; set; }

        public int RoundedCornerAngle { get; set; }
        public int LeftBarSize { get; set; }
        public int RightBarSize { get; set; }
        public int StatusBarSize { get; set; }
        public string LeftText { get; set; }
        public string StatusText { get; set; }
        public bool selected { get; set; }

        public Guid DeviceId { get; set; } // New Property for DeviceId

        public event EventHandler Cb_selector_IndexChanged;

        public event EventHandler Chk_enabled_CheckChanged;

        public event EventHandler Btn_edit_OnPressed;

        public event EventHandler Btn_delete_OnPressed;

        public event EventHandler Btn_copy_OnPressed;

        public event EventHandler Layer_OnPressed;

        private Color StatusColor1;
        private Color StatusColor2;
        private LayerType _LayerType;
        public LayerType LayerType
        {
            get { return _LayerType; }
            set
            {
                switch (value)
                {
                    case LayerType.BaseLayer:
                        StatusColor1 = Color.DimGray;
                        StatusColor2 = (Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(LayerType.BaseLayer).Value;

                        if (_cb_selector != null)
                        {
                            foreach (Enum lt in Enum.GetValues(typeof(BaseLayerType)))
                            {
                                var name = EnumExtensions.GetAttribute<LayerDisplay>(lt).Name;
                                var item = new ComboboxItem { Value = lt, Text = name };

                                _cb_selector.Items.Add(item);
                            }

                            _cb_selector.SelectedIndex = 0;
                            _cb_selector.Enabled = true;
                        }

                        if (_btn_delete != null)
                        {
                            _btn_delete.Enabled = false;
                        }

                        if (_btn_copy != null)
                        {
                            _btn_copy.Enabled = false;
                        }

                        if (_btn_edit != null)
                        {
                            _btn_edit.Enabled = false;
                        }

                        break;
                    case LayerType.DynamicLayer:
                        StatusColor1 = Color.DimGray;
                        StatusColor2 = (Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(LayerType.DynamicLayer).Value;

                        if (_cb_selector != null)
                        {
                            foreach (Enum lt in Enum.GetValues(typeof(DynamicLayerType)))
                            {
                                var name = EnumExtensions.GetAttribute<LayerDisplay>(lt).Name;
                                var item = new ComboboxItem { Value = lt, Text = name };

                                _cb_selector.Items.Add(item);
                            }

                            _cb_selector.SelectedIndex = 0;
                            _cb_selector.Enabled = true;
                        }

                        if (_btn_delete != null)
                        {
                            _btn_delete.Enabled = true;
                        }

                        if (_btn_copy != null)
                        {
                            _btn_copy.Enabled = true;
                        }

                        if (_btn_edit != null)
                        {
                            _btn_edit.Enabled = true;
                        }

                        break;
                    case LayerType.EffectLayer:
                        StatusColor1 = Color.DimGray;
                        StatusColor2 = (Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(LayerType.EffectLayer).Value;

                        if (_cb_selector != null)
                        {
                            foreach (Enum lt in Enum.GetValues(typeof(EffectLayerType)))
                            {
                                var name = EnumExtensions.GetAttribute<LayerDisplay>(lt).Name;
                                var item = new ComboboxItem { Value = lt, Text = name };

                                _cb_selector.Items.Add(item);
                            }

                            _cb_selector.SelectedIndex = 0;
                            _cb_selector.Cursor = Cursors.No;
                            _cb_selector.UseCustomForeColor = true;
                            _cb_selector.ForeColor = Color.Gray;
                            _cb_selector.Enabled = false;
                        }

                        if (_btn_delete != null)
                        {
                            _btn_delete.Enabled = false;
                        }

                        if (_btn_copy != null)
                        {
                            _btn_copy.Enabled = false;
                        }

                        if (_btn_edit != null)
                        {
                            _btn_edit.Enabled = false;
                        }

                        break;
                    default:
                        StatusColor1 = Color.DimGray;
                        StatusColor2 = Color.DimGray;
                        break;
                }

                _LayerType = value;
                StatusText = EnumExtensions.GetAttribute<DisplayAttribute>(_LayerType).Name;
            }
        }
        public MetroComboBox cb_selector
        {
            get { return _cb_selector; }
            set { _cb_selector = value; }
        }

        public CheckBox chk_enabled
        {
            get { return _chk_enabled; }
            set { _chk_enabled = value; }
        }

        public MetroButton btn_edit
        {
            get { return _btn_edit; }
            set { btn_edit = value; }
        }

        public MetroButton btn_delete
        {
            get { return _btn_delete; }
            set { btn_delete = value; }
        }

        public MetroButton btn_copy
        {
            get { return _btn_copy; }
            set { btn_copy = value; }
        }

        public bool editing
        {
            get { return _editing; }
            set
            {
                _editing = value;

                if (_editing)
                {
                    _btn_edit.Text = "Save";
                }
                else
                {
                    _btn_edit.Text = "Edit";
                }
            }
        }


        //Check radius for begin drag n drop
        public bool AllowDrag { get; set; }
        private bool _isDragging = false;
        private int _DDradius = 40;
        private int _mX = 0;
        private int _mY = 0;

        public Pn_LayerDisplay()
        {
            Font = new Font("Arial", 8);
            RoundedCornerAngle = 10;
            Margin = new Padding(0);
            LeftText = "LT";
            LeftBarSize = 30;
            StatusBarSize = 60;
            AllowDrag = true;
            StatusText = EnumExtensions.GetAttribute<DisplayAttribute>(LayerType).Name;

            _cb_selector = new MetroComboBox
            {
                Name = "cb_selector",
                Location = new Point(Padding.Left + LeftBarSize + StatusBarSize + 10, Padding.Top + Padding.Bottom + 10),
                Size = new Size(this.ClientRectangle.Width - (Padding.Left + LeftBarSize + StatusBarSize + RightBarSize + Padding.Right) + 10, this.ClientRectangle.Height - Padding.Bottom - Padding.Top),
                DisplayMember = "Text",
                ValueMember = "Value",
                Font = new Font("Arial", 8),
                DropDownWidth = 200
            };

            _chk_enabled = new CheckBox
            {
                Name = "chk_enabled",
                Location = new Point(cb_selector.Location.X + cb_selector.Size.Width + Padding.Left + 10, Padding.Top + Padding.Bottom + 18),
                Size = new Size(15, 15),
                BackColor = this.BackColor,
                Text = "Enabled",
                Checked = false
            };

            _btn_edit = new MetroButton
            {
                Name = "btn_edit",
                Location = new Point(_chk_enabled.Location.X + _chk_enabled.Size.Width + Padding.Left + 10, Padding.Top + Padding.Bottom + 18),
                Size = new Size(50, 15),
                BackColor = this.BackColor,
                ForeColor = Color.White,
                Text = "Edit",
            };

            _btn_delete = new MetroButton
            {
                Name = "btn_delete",
                Location = new Point(_btn_edit.Location.X + _btn_edit.Size.Width + Padding.Left + 10, Padding.Top + Padding.Bottom + 18),
                Size = new Size(50, 15),
                BackColor = this.BackColor,
                ForeColor = Color.White,
                Text = "Delete",
            };

            _btn_copy = new MetroButton
            {
                Name = "btn_copy",
                Location = new Point(_btn_edit.Location.X + _btn_edit.Size.Width + Padding.Left + 10, Padding.Top + Padding.Bottom + 18),
                Size = new Size(50, 15),
                BackColor = this.BackColor,
                ForeColor = Color.White,
                Text = "Copy",
            };


            this.Controls.Add(cb_selector);
            this.Controls.Add(chk_enabled);
            this.Controls.Add(_btn_edit);
            this.Controls.Add(_btn_delete);
            this.Controls.Add(_btn_copy);

            _btn_copy.Visible = false;

            this.GotFocus += new EventHandler(OnLayerPressed);
            _cb_selector.SelectedIndexChanged += new EventHandler(OnSelectedIndexChanged);
            _chk_enabled.CheckedChanged += new EventHandler(OnCheckChanged);
            _btn_edit.Click += new EventHandler(OnEditButtonPressed);
            _btn_delete.Click += new EventHandler(OnDeleteButtonPressed);
            _btn_copy.Click += new EventHandler(OnCopyButtonPressed);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe event handlers
                this.GotFocus -= new EventHandler(OnLayerPressed);
                _cb_selector.SelectedIndexChanged -= new EventHandler(OnSelectedIndexChanged);
                _chk_enabled.CheckedChanged -= new EventHandler(OnCheckChanged);
                _btn_edit.Click -= new EventHandler(OnEditButtonPressed);
                _btn_delete.Click -= new EventHandler(OnDeleteButtonPressed);
                _btn_copy.Click -= new EventHandler(OnCopyButtonPressed);

                // Dispose controls
                _cb_selector?.Dispose();
                _chk_enabled?.Dispose();
                _btn_edit?.Dispose();
                _btn_delete?.Dispose();
                _btn_copy?.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            this.BackColor = Color.SandyBrown;

            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            this.BackColor = Color.Transparent;
            this.selected = false;

            base.OnLostFocus(e);
        }

        protected override void OnClick(EventArgs e)
        {
            this.Focus();
            base.OnClick(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.Focus();
            base.OnMouseDown(e);
            _mX = e.X;
            _mY = e.Y;
            this._isDragging = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_isDragging)
            {
                // This is a check to see if the mouse is moving while pressed.
                // Without this, the DragDrop is fired directly when the control is clicked, now you have to drag a few pixels first.
                if (e.Button == MouseButtons.Left && _DDradius > 0 && this.AllowDrag)
                {
                    int num1 = _mX - e.X;
                    int num2 = _mY - e.Y;
                    if (((num1 * num1) + (num2 * num2)) > _DDradius)
                    {
                        DoDragDrop(this, DragDropEffects.All);
                        _isDragging = true;
                        return;
                    }
                }
                base.OnMouseMove(e);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _isDragging = false;
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            this.SuspendLayout();
            base.OnPaint(e);
            RePaint();
            this.ResumeLayout(true);
        }



        public void RePaint()
        {
            LinearGradientBrush _leftAndRightBrush = new LinearGradientBrush(GetMainArea(), Color.DimGray, Color.Black, LinearGradientMode.Vertical);
            LinearGradientBrush _statusBrush = new LinearGradientBrush(GetMainArea(), StatusColor1, StatusColor2, LinearGradientMode.Vertical);
            LinearGradientBrush _mainBrush = new LinearGradientBrush(GetMainArea(), Color.DimGray, Color.SlateGray, LinearGradientMode.Vertical);
            StringFormat _stringFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            using (var graphics = this.CreateGraphics())
            {
                if (editing)
                {
                    _leftAndRightBrush = new LinearGradientBrush(GetMainArea(), Color.DimGray, Color.Black, LinearGradientMode.Vertical);
                    _statusBrush = new LinearGradientBrush(GetMainArea(), StatusColor1, StatusColor2, LinearGradientMode.Vertical);
                    _mainBrush = new LinearGradientBrush(GetMainArea(), Color.DimGray, Color.Orange, LinearGradientMode.Vertical);
                }

                if (LeftBarSize > 0)
                {
                    graphics.FillRoundedRectangle(_leftAndRightBrush, this.GetLeftArea(), this.RoundedCornerAngle, RectangleEdgeFilter.TopLeft | RectangleEdgeFilter.BottomLeft);
                    graphics.DrawString(this.LeftText, this.Font, Brushes.White, this.GetLeftArea(), _stringFormat);
                }

                if (StatusBarSize > 0)
                {
                    graphics.FillRoundedRectangle(_statusBrush, this.GetStatusArea(), this.RoundedCornerAngle, RectangleEdgeFilter.None);
                    graphics.DrawString(this.StatusText, this.Font, Brushes.White, this.GetStatusArea(), _stringFormat);
                }
                graphics.FillRoundedRectangle(Brushes.DimGray, GetMainAreaBackground(), this.RoundedCornerAngle, RectangleEdgeFilter.None);
                graphics.FillRoundedRectangle(_mainBrush, this.GetMainArea(), this.RoundedCornerAngle, RectangleEdgeFilter.None);
            }
        }

        private Rectangle GetLeftArea()
        {
            return new Rectangle(
                Padding.Left,
                Padding.Top,
                LeftBarSize,
                this.ClientRectangle.Height - Padding.Bottom - Padding.Top);
        }

        private Rectangle GetStatusArea()
        {
            return new Rectangle(
                Padding.Left + LeftBarSize,
                Padding.Top,
                StatusBarSize,
                this.ClientRectangle.Height - Padding.Bottom - Padding.Top);
        }

        private Rectangle GetMainArea()
        {
            return new Rectangle(
                Padding.Left + LeftBarSize + StatusBarSize,
                   Padding.Top,
                   this.ClientRectangle.Width - (Padding.Left + LeftBarSize + StatusBarSize + RightBarSize + Padding.Right),
                   this.ClientRectangle.Height - Padding.Bottom - Padding.Top);
        }

        private Rectangle GetMainAreaBackground()
        {
            return new Rectangle(
                   Padding.Left + LeftBarSize + StatusBarSize,
                   Padding.Top,
                   this.ClientRectangle.Width - (Padding.Left + LeftBarSize + StatusBarSize + RightBarSize + Padding.Right),
                   this.ClientRectangle.Height - Padding.Bottom - Padding.Top);
        }

        private void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (Cb_selector_IndexChanged != null)
                Cb_selector_IndexChanged(sender, e);
        }

        private void OnCheckChanged(object sender, EventArgs e)
        {
            if (Chk_enabled_CheckChanged != null)
                Chk_enabled_CheckChanged(sender, e);
        }

        private void OnEditButtonPressed(object sender, EventArgs e)
        {
            if (Btn_edit_OnPressed != null)
                Btn_edit_OnPressed(sender, e);
        }

        private void OnDeleteButtonPressed(object sender, EventArgs e)
        {
            if (Btn_delete_OnPressed != null)
                Btn_delete_OnPressed(sender, e);
        }

        private void OnCopyButtonPressed(object sender, EventArgs e)
        {
            if (Btn_copy_OnPressed != null)
                Btn_copy_OnPressed(sender, e);
        }

        private void OnLayerPressed(object sender, EventArgs e)
        {
            if (Layer_OnPressed != null)
                Layer_OnPressed(this, e);
        }

    }
}
