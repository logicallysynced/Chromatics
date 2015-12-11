// reference:Corale.Colore.dll
// reference:PresentationCore.dll
// reference:System.Core.dll
// reference:CUE.NET.dll
// reference:System.IO.Compression.dll
// reference:System.IO.Compression.FileSystem.dll

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Runtime.InteropServices;
using Advanced_Combat_Tracker;
using Corale.Colore.Core;
using System.Diagnostics;
using LedCSharp;
using Corale.Colore.Razer.Mouse;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CUE.NET;
using CUE.NET.Brushes;
using CUE.NET.Devices.Keyboard;
using CUE.NET.Devices.Keyboard.Enums;
using CUE.NET.Devices.Keyboard.Keys;
using CUE.NET.Devices.Headset;
using CUE.NET.Devices.Mouse;
using CUE.NET.Exceptions;
using System.Reflection;
using System.Net;

namespace Chromatics
{
    public class Chromatics : UserControl, IActPluginV1
    {
        
        #region Designer Created Code (Avoid editing)
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.chk_enableEmnity = new System.Windows.Forms.CheckBox();
            this.chk_enableTriggers = new System.Windows.Forms.CheckBox();
            this.lbl_defaultCol = new System.Windows.Forms.Label();
            this.lbl_emnityCol = new System.Windows.Forms.Label();
            this.gp_box1 = new System.Windows.Forms.GroupBox();
            this.lbl_NotifyDPS = new System.Windows.Forms.Label();
            this.txt_DPSNotify = new System.Windows.Forms.NumericUpDown();
            this.lbl_dpsMin = new System.Windows.Forms.Label();
            this.txt_DPSlimit = new System.Windows.Forms.NumericUpDown();
            this.btn_DPSLimitCol = new System.Windows.Forms.Button();
            this.lbl_alertCol = new System.Windows.Forms.Label();
            this.chk_DPSLimit = new System.Windows.Forms.CheckBox();
            this.lbl_TimerEvent = new System.Windows.Forms.Label();
            this.cb_Event = new System.Windows.Forms.ComboBox();
            this.lbl_TimerCount = new System.Windows.Forms.Label();
            this.cb_TimerCount = new System.Windows.Forms.ComboBox();
            this.btn_timerCol = new System.Windows.Forms.Button();
            this.lbl_TimerCol = new System.Windows.Forms.Label();
            this.chk_enableTimers = new System.Windows.Forms.CheckBox();
            this.lbl_triggerSpeed = new System.Windows.Forms.Label();
            this.cb_TriggerSpeed = new System.Windows.Forms.ComboBox();
            this.lbl_triggerCount = new System.Windows.Forms.Label();
            this.cb_TriggerCount = new System.Windows.Forms.ComboBox();
            this.btn_triggerCol = new System.Windows.Forms.Button();
            this.lbl_trigCol = new System.Windows.Forms.Label();
            this.btn_emnityCol = new System.Windows.Forms.Button();
            this.btn_defaultCol = new System.Windows.Forms.Button();
            this.dia_col1 = new System.Windows.Forms.ColorDialog();
            this.dia_col2 = new System.Windows.Forms.ColorDialog();
            this.gp_box2 = new System.Windows.Forms.GroupBox();
            this.chk_deviceCorsair = new System.Windows.Forms.CheckBox();
            this.chk_deviceLogitech = new System.Windows.Forms.CheckBox();
            this.btn_rescan = new System.Windows.Forms.Button();
            this.chk_deviceMousepad = new System.Windows.Forms.CheckBox();
            this.chk_deviceHeadset = new System.Windows.Forms.CheckBox();
            this.chk_deviceMouse = new System.Windows.Forms.CheckBox();
            this.chk_deviceKeypad = new System.Windows.Forms.CheckBox();
            this.chk_deviceKeyboard = new System.Windows.Forms.CheckBox();
            this.dia_col3 = new System.Windows.Forms.ColorDialog();
            this.gp_box3 = new System.Windows.Forms.GroupBox();
            this.btn_ls8Col = new System.Windows.Forms.Button();
            this.btn_ls7Col = new System.Windows.Forms.Button();
            this.btn_ls6Col = new System.Windows.Forms.Button();
            this.btn_ls5Col = new System.Windows.Forms.Button();
            this.btn_ls4Col = new System.Windows.Forms.Button();
            this.btn_ls3Col = new System.Windows.Forms.Button();
            this.btn_ls2Col = new System.Windows.Forms.Button();
            this.btn_ls1Col = new System.Windows.Forms.Button();
            this.btn_fcCol = new System.Windows.Forms.Button();
            this.btn_allianceCol = new System.Windows.Forms.Button();
            this.btn_partyCol = new System.Windows.Forms.Button();
            this.btn_yellCol = new System.Windows.Forms.Button();
            this.btn_shoutCol = new System.Windows.Forms.Button();
            this.btn_tellCol = new System.Windows.Forms.Button();
            this.btn_sayCol = new System.Windows.Forms.Button();
            this.chk_ls8 = new System.Windows.Forms.CheckBox();
            this.chk_fc = new System.Windows.Forms.CheckBox();
            this.chk_alliance = new System.Windows.Forms.CheckBox();
            this.chk_ls7 = new System.Windows.Forms.CheckBox();
            this.chk_party = new System.Windows.Forms.CheckBox();
            this.chk_yell = new System.Windows.Forms.CheckBox();
            this.chk_ls6 = new System.Windows.Forms.CheckBox();
            this.chk_shout = new System.Windows.Forms.CheckBox();
            this.chk_tell = new System.Windows.Forms.CheckBox();
            this.chk_ls5 = new System.Windows.Forms.CheckBox();
            this.chk_say = new System.Windows.Forms.CheckBox();
            this.chk_ls4 = new System.Windows.Forms.CheckBox();
            this.chk_ls1 = new System.Windows.Forms.CheckBox();
            this.chk_ls3 = new System.Windows.Forms.CheckBox();
            this.chk_ls2 = new System.Windows.Forms.CheckBox();
            this.dia_say = new System.Windows.Forms.ColorDialog();
            this.dia_tell = new System.Windows.Forms.ColorDialog();
            this.dia_shout = new System.Windows.Forms.ColorDialog();
            this.dia_yell = new System.Windows.Forms.ColorDialog();
            this.dia_party = new System.Windows.Forms.ColorDialog();
            this.dia_alliance = new System.Windows.Forms.ColorDialog();
            this.dia_ls1 = new System.Windows.Forms.ColorDialog();
            this.dia_ls2 = new System.Windows.Forms.ColorDialog();
            this.dia_ls3 = new System.Windows.Forms.ColorDialog();
            this.dia_ls4 = new System.Windows.Forms.ColorDialog();
            this.dia_ls5 = new System.Windows.Forms.ColorDialog();
            this.dia_ls6 = new System.Windows.Forms.ColorDialog();
            this.dia_ls7 = new System.Windows.Forms.ColorDialog();
            this.dia_ls8 = new System.Windows.Forms.ColorDialog();
            this.dia_fc = new System.Windows.Forms.ColorDialog();
            this.gp_box4 = new System.Windows.Forms.GroupBox();
            this.chk_reactiveWeather = new System.Windows.Forms.CheckBox();
            this.btn_restoreDefaults = new System.Windows.Forms.Button();
            this.btn_raidEffectsB = new System.Windows.Forms.Button();
            this.btn_raidEffectsA = new System.Windows.Forms.Button();
            this.chk_raidEffects = new System.Windows.Forms.CheckBox();
            this.chk_GoldSaucerVegas = new System.Windows.Forms.CheckBox();
            this.dia_raidA = new System.Windows.Forms.ColorDialog();
            this.dia_raidB = new System.Windows.Forms.ColorDialog();
            this.dia_col4 = new System.Windows.Forms.ColorDialog();
            this.pic_Logo = new System.Windows.Forms.PictureBox();
            this.dia_DPSlimit = new System.Windows.Forms.ColorDialog();
            this.gp_box1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txt_DPSNotify)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_DPSlimit)).BeginInit();
            this.gp_box2.SuspendLayout();
            this.gp_box3.SuspendLayout();
            this.gp_box4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pic_Logo)).BeginInit();
            this.SuspendLayout();
            // 
            // chk_enableEmnity
            // 
            this.chk_enableEmnity.AutoSize = true;
            this.chk_enableEmnity.Location = new System.Drawing.Point(37, 28);
            this.chk_enableEmnity.Name = "chk_enableEmnity";
            this.chk_enableEmnity.Size = new System.Drawing.Size(133, 17);
            this.chk_enableEmnity.TabIndex = 0;
            this.chk_enableEmnity.Text = "Enable Enmity Change";
            this.chk_enableEmnity.UseVisualStyleBackColor = true;
            this.chk_enableEmnity.CheckedChanged += new System.EventHandler(this.chk_enableEmnity_CheckedChanged);
            // 
            // chk_enableTriggers
            // 
            this.chk_enableTriggers.AutoSize = true;
            this.chk_enableTriggers.Location = new System.Drawing.Point(230, 84);
            this.chk_enableTriggers.Name = "chk_enableTriggers";
            this.chk_enableTriggers.Size = new System.Drawing.Size(162, 17);
            this.chk_enableTriggers.TabIndex = 1;
            this.chk_enableTriggers.Text = "Enable Custom Trigger Alerts";
            this.chk_enableTriggers.UseVisualStyleBackColor = true;
            this.chk_enableTriggers.CheckedChanged += new System.EventHandler(this.chk_enableTriggers_CheckedChanged);
            // 
            // lbl_defaultCol
            // 
            this.lbl_defaultCol.AutoSize = true;
            this.lbl_defaultCol.Location = new System.Drawing.Point(37, 74);
            this.lbl_defaultCol.Name = "lbl_defaultCol";
            this.lbl_defaultCol.Size = new System.Drawing.Size(68, 13);
            this.lbl_defaultCol.TabIndex = 3;
            this.lbl_defaultCol.Text = "Default Color";
            // 
            // lbl_emnityCol
            // 
            this.lbl_emnityCol.AutoSize = true;
            this.lbl_emnityCol.Location = new System.Drawing.Point(37, 116);
            this.lbl_emnityCol.Name = "lbl_emnityCol";
            this.lbl_emnityCol.Size = new System.Drawing.Size(65, 13);
            this.lbl_emnityCol.TabIndex = 5;
            this.lbl_emnityCol.Text = "Emnity Color";
            // 
            // gp_box1
            // 
            this.gp_box1.Controls.Add(this.lbl_NotifyDPS);
            this.gp_box1.Controls.Add(this.txt_DPSNotify);
            this.gp_box1.Controls.Add(this.lbl_dpsMin);
            this.gp_box1.Controls.Add(this.txt_DPSlimit);
            this.gp_box1.Controls.Add(this.btn_DPSLimitCol);
            this.gp_box1.Controls.Add(this.lbl_alertCol);
            this.gp_box1.Controls.Add(this.chk_DPSLimit);
            this.gp_box1.Controls.Add(this.lbl_TimerEvent);
            this.gp_box1.Controls.Add(this.cb_Event);
            this.gp_box1.Controls.Add(this.lbl_TimerCount);
            this.gp_box1.Controls.Add(this.cb_TimerCount);
            this.gp_box1.Controls.Add(this.btn_timerCol);
            this.gp_box1.Controls.Add(this.lbl_TimerCol);
            this.gp_box1.Controls.Add(this.chk_enableTimers);
            this.gp_box1.Controls.Add(this.lbl_triggerSpeed);
            this.gp_box1.Controls.Add(this.cb_TriggerSpeed);
            this.gp_box1.Controls.Add(this.lbl_triggerCount);
            this.gp_box1.Controls.Add(this.cb_TriggerCount);
            this.gp_box1.Controls.Add(this.btn_triggerCol);
            this.gp_box1.Controls.Add(this.lbl_trigCol);
            this.gp_box1.Controls.Add(this.btn_emnityCol);
            this.gp_box1.Controls.Add(this.btn_defaultCol);
            this.gp_box1.Controls.Add(this.chk_enableEmnity);
            this.gp_box1.Controls.Add(this.lbl_emnityCol);
            this.gp_box1.Controls.Add(this.chk_enableTriggers);
            this.gp_box1.Controls.Add(this.lbl_defaultCol);
            this.gp_box1.Location = new System.Drawing.Point(13, 15);
            this.gp_box1.Name = "gp_box1";
            this.gp_box1.Size = new System.Drawing.Size(608, 319);
            this.gp_box1.TabIndex = 6;
            this.gp_box1.TabStop = false;
            this.gp_box1.Text = "Battle Alerts";
            // 
            // lbl_NotifyDPS
            // 
            this.lbl_NotifyDPS.AutoSize = true;
            this.lbl_NotifyDPS.Location = new System.Drawing.Point(37, 240);
            this.lbl_NotifyDPS.Name = "lbl_NotifyDPS";
            this.lbl_NotifyDPS.Size = new System.Drawing.Size(59, 13);
            this.lbl_NotifyDPS.TabIndex = 27;
            this.lbl_NotifyDPS.Text = "Notify DPS";
            // 
            // txt_DPSNotify
            // 
            this.txt_DPSNotify.Location = new System.Drawing.Point(111, 237);
            this.txt_DPSNotify.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.txt_DPSNotify.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.txt_DPSNotify.Name = "txt_DPSNotify";
            this.txt_DPSNotify.Size = new System.Drawing.Size(75, 20);
            this.txt_DPSNotify.TabIndex = 26;
            this.txt_DPSNotify.ValueChanged += new System.EventHandler(this.txt_DPSNotify_ValueChanged);
            // 
            // lbl_dpsMin
            // 
            this.lbl_dpsMin.AutoSize = true;
            this.lbl_dpsMin.Location = new System.Drawing.Point(37, 209);
            this.lbl_dpsMin.Name = "lbl_dpsMin";
            this.lbl_dpsMin.Size = new System.Drawing.Size(63, 13);
            this.lbl_dpsMin.TabIndex = 25;
            this.lbl_dpsMin.Text = "Target DPS";
            // 
            // txt_DPSlimit
            // 
            this.txt_DPSlimit.Location = new System.Drawing.Point(111, 206);
            this.txt_DPSlimit.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.txt_DPSlimit.Name = "txt_DPSlimit";
            this.txt_DPSlimit.Size = new System.Drawing.Size(75, 20);
            this.txt_DPSlimit.TabIndex = 24;
            this.txt_DPSlimit.ValueChanged += new System.EventHandler(this.txt_DPSlimit_ValueChanged);
            // 
            // btn_DPSLimitCol
            // 
            this.btn_DPSLimitCol.BackColor = System.Drawing.Color.DarkOrange;
            this.btn_DPSLimitCol.ForeColor = System.Drawing.Color.DarkOrange;
            this.btn_DPSLimitCol.Location = new System.Drawing.Point(111, 268);
            this.btn_DPSLimitCol.Name = "btn_DPSLimitCol";
            this.btn_DPSLimitCol.Size = new System.Drawing.Size(75, 23);
            this.btn_DPSLimitCol.TabIndex = 23;
            this.btn_DPSLimitCol.UseVisualStyleBackColor = false;
            this.btn_DPSLimitCol.Click += new System.EventHandler(this.btn_DPSLimitCol_Click);
            // 
            // lbl_alertCol
            // 
            this.lbl_alertCol.AutoSize = true;
            this.lbl_alertCol.Location = new System.Drawing.Point(37, 273);
            this.lbl_alertCol.Name = "lbl_alertCol";
            this.lbl_alertCol.Size = new System.Drawing.Size(55, 13);
            this.lbl_alertCol.TabIndex = 22;
            this.lbl_alertCol.Text = "Alert Color";
            // 
            // chk_DPSLimit
            // 
            this.chk_DPSLimit.AutoSize = true;
            this.chk_DPSLimit.Location = new System.Drawing.Point(37, 171);
            this.chk_DPSLimit.Name = "chk_DPSLimit";
            this.chk_DPSLimit.Size = new System.Drawing.Size(134, 17);
            this.chk_DPSLimit.TabIndex = 21;
            this.chk_DPSLimit.Text = "Enable DPS Threshold";
            this.chk_DPSLimit.UseVisualStyleBackColor = true;
            this.chk_DPSLimit.CheckedChanged += new System.EventHandler(this.chk_DPSLimit_CheckedChanged);
            // 
            // lbl_TimerEvent
            // 
            this.lbl_TimerEvent.AutoSize = true;
            this.lbl_TimerEvent.Location = new System.Drawing.Point(423, 216);
            this.lbl_TimerEvent.Name = "lbl_TimerEvent";
            this.lbl_TimerEvent.Size = new System.Drawing.Size(71, 13);
            this.lbl_TimerEvent.TabIndex = 20;
            this.lbl_TimerEvent.Text = "Trigger Event";
            // 
            // cb_Event
            // 
            this.cb_Event.FormattingEnabled = true;
            this.cb_Event.Items.AddRange(new object[] {
            "Expire",
            "Warning",
            "Removed"});
            this.cb_Event.Location = new System.Drawing.Point(498, 213);
            this.cb_Event.Name = "cb_Event";
            this.cb_Event.Size = new System.Drawing.Size(75, 21);
            this.cb_Event.TabIndex = 19;
            this.cb_Event.SelectedIndexChanged += new System.EventHandler(this.cb_Event_SelectedIndexChanged);
            // 
            // lbl_TimerCount
            // 
            this.lbl_TimerCount.AutoSize = true;
            this.lbl_TimerCount.Location = new System.Drawing.Point(423, 174);
            this.lbl_TimerCount.Name = "lbl_TimerCount";
            this.lbl_TimerCount.Size = new System.Drawing.Size(63, 13);
            this.lbl_TimerCount.TabIndex = 18;
            this.lbl_TimerCount.Text = "Flash Count";
            // 
            // cb_TimerCount
            // 
            this.cb_TimerCount.FormattingEnabled = true;
            this.cb_TimerCount.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5"});
            this.cb_TimerCount.Location = new System.Drawing.Point(498, 171);
            this.cb_TimerCount.Name = "cb_TimerCount";
            this.cb_TimerCount.Size = new System.Drawing.Size(75, 21);
            this.cb_TimerCount.TabIndex = 17;
            this.cb_TimerCount.SelectedIndexChanged += new System.EventHandler(this.cb_TimerCount_SelectedIndexChanged);
            // 
            // btn_timerCol
            // 
            this.btn_timerCol.BackColor = System.Drawing.Color.BlueViolet;
            this.btn_timerCol.ForeColor = System.Drawing.Color.BlueViolet;
            this.btn_timerCol.Location = new System.Drawing.Point(498, 127);
            this.btn_timerCol.Name = "btn_timerCol";
            this.btn_timerCol.Size = new System.Drawing.Size(75, 23);
            this.btn_timerCol.TabIndex = 16;
            this.btn_timerCol.UseVisualStyleBackColor = false;
            this.btn_timerCol.Click += new System.EventHandler(this.btn_timerCol_Click);
            // 
            // lbl_TimerCol
            // 
            this.lbl_TimerCol.AutoSize = true;
            this.lbl_TimerCol.Location = new System.Drawing.Point(423, 132);
            this.lbl_TimerCol.Name = "lbl_TimerCol";
            this.lbl_TimerCol.Size = new System.Drawing.Size(60, 13);
            this.lbl_TimerCol.TabIndex = 15;
            this.lbl_TimerCol.Text = "Timer Color";
            // 
            // chk_enableTimers
            // 
            this.chk_enableTimers.AutoSize = true;
            this.chk_enableTimers.Location = new System.Drawing.Point(426, 84);
            this.chk_enableTimers.Name = "chk_enableTimers";
            this.chk_enableTimers.Size = new System.Drawing.Size(117, 17);
            this.chk_enableTimers.TabIndex = 14;
            this.chk_enableTimers.Text = "Enable Timer Alerts";
            this.chk_enableTimers.UseVisualStyleBackColor = true;
            this.chk_enableTimers.CheckedChanged += new System.EventHandler(this.chk_enableTimers_CheckedChanged);
            // 
            // lbl_triggerSpeed
            // 
            this.lbl_triggerSpeed.AutoSize = true;
            this.lbl_triggerSpeed.Location = new System.Drawing.Point(227, 216);
            this.lbl_triggerSpeed.Name = "lbl_triggerSpeed";
            this.lbl_triggerSpeed.Size = new System.Drawing.Size(66, 13);
            this.lbl_triggerSpeed.TabIndex = 13;
            this.lbl_triggerSpeed.Text = "Flash Speed";
            // 
            // cb_TriggerSpeed
            // 
            this.cb_TriggerSpeed.FormattingEnabled = true;
            this.cb_TriggerSpeed.Items.AddRange(new object[] {
            "Very Slow",
            "Slow",
            "Moderate",
            "Fast",
            "Very Fast"});
            this.cb_TriggerSpeed.Location = new System.Drawing.Point(296, 213);
            this.cb_TriggerSpeed.Name = "cb_TriggerSpeed";
            this.cb_TriggerSpeed.Size = new System.Drawing.Size(75, 21);
            this.cb_TriggerSpeed.TabIndex = 12;
            this.cb_TriggerSpeed.SelectedIndexChanged += new System.EventHandler(this.cb_TriggerSpeed_SelectedIndexChanged);
            // 
            // lbl_triggerCount
            // 
            this.lbl_triggerCount.AutoSize = true;
            this.lbl_triggerCount.Location = new System.Drawing.Point(227, 174);
            this.lbl_triggerCount.Name = "lbl_triggerCount";
            this.lbl_triggerCount.Size = new System.Drawing.Size(63, 13);
            this.lbl_triggerCount.TabIndex = 11;
            this.lbl_triggerCount.Text = "Flash Count";
            // 
            // cb_TriggerCount
            // 
            this.cb_TriggerCount.FormattingEnabled = true;
            this.cb_TriggerCount.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5"});
            this.cb_TriggerCount.Location = new System.Drawing.Point(296, 171);
            this.cb_TriggerCount.Name = "cb_TriggerCount";
            this.cb_TriggerCount.Size = new System.Drawing.Size(75, 21);
            this.cb_TriggerCount.TabIndex = 10;
            this.cb_TriggerCount.SelectedIndexChanged += new System.EventHandler(this.cb_TriggerCount_SelectedIndexChanged);
            // 
            // btn_triggerCol
            // 
            this.btn_triggerCol.BackColor = System.Drawing.Color.BlueViolet;
            this.btn_triggerCol.ForeColor = System.Drawing.Color.BlueViolet;
            this.btn_triggerCol.Location = new System.Drawing.Point(296, 127);
            this.btn_triggerCol.Name = "btn_triggerCol";
            this.btn_triggerCol.Size = new System.Drawing.Size(75, 23);
            this.btn_triggerCol.TabIndex = 9;
            this.btn_triggerCol.UseVisualStyleBackColor = false;
            this.btn_triggerCol.Click += new System.EventHandler(this.btn_triggerCol_Click);
            // 
            // lbl_trigCol
            // 
            this.lbl_trigCol.AutoSize = true;
            this.lbl_trigCol.Location = new System.Drawing.Point(227, 132);
            this.lbl_trigCol.Name = "lbl_trigCol";
            this.lbl_trigCol.Size = new System.Drawing.Size(67, 13);
            this.lbl_trigCol.TabIndex = 8;
            this.lbl_trigCol.Text = "Trigger Color";
            // 
            // btn_emnityCol
            // 
            this.btn_emnityCol.BackColor = System.Drawing.Color.Red;
            this.btn_emnityCol.ForeColor = System.Drawing.Color.Red;
            this.btn_emnityCol.Location = new System.Drawing.Point(111, 111);
            this.btn_emnityCol.Name = "btn_emnityCol";
            this.btn_emnityCol.Size = new System.Drawing.Size(75, 23);
            this.btn_emnityCol.TabIndex = 7;
            this.btn_emnityCol.UseVisualStyleBackColor = false;
            this.btn_emnityCol.Click += new System.EventHandler(this.btn_emnityCol_Click);
            // 
            // btn_defaultCol
            // 
            this.btn_defaultCol.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.btn_defaultCol.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.btn_defaultCol.Location = new System.Drawing.Point(111, 69);
            this.btn_defaultCol.Name = "btn_defaultCol";
            this.btn_defaultCol.Size = new System.Drawing.Size(75, 23);
            this.btn_defaultCol.TabIndex = 6;
            this.btn_defaultCol.UseVisualStyleBackColor = false;
            this.btn_defaultCol.Click += new System.EventHandler(this.btn_defaultCol_Click);
            // 
            // dia_col1
            // 
            this.dia_col1.AnyColor = true;
            this.dia_col1.FullOpen = true;
            // 
            // dia_col2
            // 
            this.dia_col2.AnyColor = true;
            this.dia_col2.FullOpen = true;
            // 
            // gp_box2
            // 
            this.gp_box2.Controls.Add(this.chk_deviceCorsair);
            this.gp_box2.Controls.Add(this.chk_deviceLogitech);
            this.gp_box2.Controls.Add(this.btn_rescan);
            this.gp_box2.Controls.Add(this.chk_deviceMousepad);
            this.gp_box2.Controls.Add(this.chk_deviceHeadset);
            this.gp_box2.Controls.Add(this.chk_deviceMouse);
            this.gp_box2.Controls.Add(this.chk_deviceKeypad);
            this.gp_box2.Controls.Add(this.chk_deviceKeyboard);
            this.gp_box2.Location = new System.Drawing.Point(641, 15);
            this.gp_box2.Name = "gp_box2";
            this.gp_box2.Size = new System.Drawing.Size(232, 319);
            this.gp_box2.TabIndex = 7;
            this.gp_box2.TabStop = false;
            this.gp_box2.Text = "Devices";
            // 
            // chk_deviceCorsair
            // 
            this.chk_deviceCorsair.AutoCheck = false;
            this.chk_deviceCorsair.AutoSize = true;
            this.chk_deviceCorsair.ForeColor = System.Drawing.Color.LightGray;
            this.chk_deviceCorsair.Location = new System.Drawing.Point(48, 194);
            this.chk_deviceCorsair.Margin = new System.Windows.Forms.Padding(8);
            this.chk_deviceCorsair.Name = "chk_deviceCorsair";
            this.chk_deviceCorsair.Size = new System.Drawing.Size(100, 17);
            this.chk_deviceCorsair.TabIndex = 8;
            this.chk_deviceCorsair.Text = "Corsair Devices";
            this.chk_deviceCorsair.UseVisualStyleBackColor = true;
            this.chk_deviceCorsair.CheckedChanged += new System.EventHandler(this.chk_deviceCorsair_CheckedChanged);
            // 
            // chk_deviceLogitech
            // 
            this.chk_deviceLogitech.AutoCheck = false;
            this.chk_deviceLogitech.AutoSize = true;
            this.chk_deviceLogitech.ForeColor = System.Drawing.Color.LightGray;
            this.chk_deviceLogitech.Location = new System.Drawing.Point(48, 166);
            this.chk_deviceLogitech.Margin = new System.Windows.Forms.Padding(8);
            this.chk_deviceLogitech.Name = "chk_deviceLogitech";
            this.chk_deviceLogitech.Size = new System.Drawing.Size(109, 17);
            this.chk_deviceLogitech.TabIndex = 6;
            this.chk_deviceLogitech.Text = "Logitech Devices";
            this.chk_deviceLogitech.UseVisualStyleBackColor = true;
            this.chk_deviceLogitech.CheckedChanged += new System.EventHandler(this.chk_deviceLogitech_CheckedChanged);
            // 
            // btn_rescan
            // 
            this.btn_rescan.Location = new System.Drawing.Point(77, 223);
            this.btn_rescan.Name = "btn_rescan";
            this.btn_rescan.Size = new System.Drawing.Size(75, 23);
            this.btn_rescan.TabIndex = 5;
            this.btn_rescan.Text = "Rescan";
            this.btn_rescan.UseVisualStyleBackColor = true;
            this.btn_rescan.Click += new System.EventHandler(this.btn_rescan_Click);
            // 
            // chk_deviceMousepad
            // 
            this.chk_deviceMousepad.AutoCheck = false;
            this.chk_deviceMousepad.AutoSize = true;
            this.chk_deviceMousepad.ForeColor = System.Drawing.Color.LightGray;
            this.chk_deviceMousepad.Location = new System.Drawing.Point(48, 138);
            this.chk_deviceMousepad.Margin = new System.Windows.Forms.Padding(8);
            this.chk_deviceMousepad.Name = "chk_deviceMousepad";
            this.chk_deviceMousepad.Size = new System.Drawing.Size(146, 17);
            this.chk_deviceMousepad.TabIndex = 4;
            this.chk_deviceMousepad.Text = "Razer Chroma Mousepad";
            this.chk_deviceMousepad.UseVisualStyleBackColor = true;
            this.chk_deviceMousepad.CheckedChanged += new System.EventHandler(this.chk_deviceMousepad_CheckedChanged);
            // 
            // chk_deviceHeadset
            // 
            this.chk_deviceHeadset.AutoCheck = false;
            this.chk_deviceHeadset.AutoSize = true;
            this.chk_deviceHeadset.ForeColor = System.Drawing.Color.LightGray;
            this.chk_deviceHeadset.Location = new System.Drawing.Point(48, 110);
            this.chk_deviceHeadset.Margin = new System.Windows.Forms.Padding(8);
            this.chk_deviceHeadset.Name = "chk_deviceHeadset";
            this.chk_deviceHeadset.Size = new System.Drawing.Size(136, 17);
            this.chk_deviceHeadset.TabIndex = 3;
            this.chk_deviceHeadset.Text = "Razer Chroma Headset";
            this.chk_deviceHeadset.UseVisualStyleBackColor = true;
            this.chk_deviceHeadset.CheckedChanged += new System.EventHandler(this.chk_deviceHeadset_CheckedChanged);
            // 
            // chk_deviceMouse
            // 
            this.chk_deviceMouse.AutoCheck = false;
            this.chk_deviceMouse.AutoSize = true;
            this.chk_deviceMouse.ForeColor = System.Drawing.Color.LightGray;
            this.chk_deviceMouse.Location = new System.Drawing.Point(48, 82);
            this.chk_deviceMouse.Margin = new System.Windows.Forms.Padding(8);
            this.chk_deviceMouse.Name = "chk_deviceMouse";
            this.chk_deviceMouse.Size = new System.Drawing.Size(128, 17);
            this.chk_deviceMouse.TabIndex = 2;
            this.chk_deviceMouse.Text = "Razer Chroma Mouse";
            this.chk_deviceMouse.UseVisualStyleBackColor = true;
            this.chk_deviceMouse.CheckedChanged += new System.EventHandler(this.chk_deviceMouse_CheckedChanged);
            // 
            // chk_deviceKeypad
            // 
            this.chk_deviceKeypad.AutoCheck = false;
            this.chk_deviceKeypad.AutoSize = true;
            this.chk_deviceKeypad.ForeColor = System.Drawing.Color.LightGray;
            this.chk_deviceKeypad.Location = new System.Drawing.Point(48, 54);
            this.chk_deviceKeypad.Margin = new System.Windows.Forms.Padding(8);
            this.chk_deviceKeypad.Name = "chk_deviceKeypad";
            this.chk_deviceKeypad.Size = new System.Drawing.Size(132, 17);
            this.chk_deviceKeypad.TabIndex = 1;
            this.chk_deviceKeypad.Text = "Razer Chroma Keypad";
            this.chk_deviceKeypad.UseVisualStyleBackColor = true;
            this.chk_deviceKeypad.CheckedChanged += new System.EventHandler(this.chk_deviceKeypad_CheckedChanged);
            // 
            // chk_deviceKeyboard
            // 
            this.chk_deviceKeyboard.AutoCheck = false;
            this.chk_deviceKeyboard.AutoSize = true;
            this.chk_deviceKeyboard.ForeColor = System.Drawing.Color.LightGray;
            this.chk_deviceKeyboard.Location = new System.Drawing.Point(48, 26);
            this.chk_deviceKeyboard.Margin = new System.Windows.Forms.Padding(8);
            this.chk_deviceKeyboard.Name = "chk_deviceKeyboard";
            this.chk_deviceKeyboard.Size = new System.Drawing.Size(141, 17);
            this.chk_deviceKeyboard.TabIndex = 0;
            this.chk_deviceKeyboard.Text = "Razer Chroma Keyboard";
            this.chk_deviceKeyboard.UseVisualStyleBackColor = true;
            this.chk_deviceKeyboard.CheckedChanged += new System.EventHandler(this.chk_deviceKeyboard_CheckedChanged);
            // 
            // dia_col3
            // 
            this.dia_col3.AnyColor = true;
            this.dia_col3.FullOpen = true;
            // 
            // gp_box3
            // 
            this.gp_box3.Controls.Add(this.btn_ls8Col);
            this.gp_box3.Controls.Add(this.btn_ls7Col);
            this.gp_box3.Controls.Add(this.btn_ls6Col);
            this.gp_box3.Controls.Add(this.btn_ls5Col);
            this.gp_box3.Controls.Add(this.btn_ls4Col);
            this.gp_box3.Controls.Add(this.btn_ls3Col);
            this.gp_box3.Controls.Add(this.btn_ls2Col);
            this.gp_box3.Controls.Add(this.btn_ls1Col);
            this.gp_box3.Controls.Add(this.btn_fcCol);
            this.gp_box3.Controls.Add(this.btn_allianceCol);
            this.gp_box3.Controls.Add(this.btn_partyCol);
            this.gp_box3.Controls.Add(this.btn_yellCol);
            this.gp_box3.Controls.Add(this.btn_shoutCol);
            this.gp_box3.Controls.Add(this.btn_tellCol);
            this.gp_box3.Controls.Add(this.btn_sayCol);
            this.gp_box3.Controls.Add(this.chk_ls8);
            this.gp_box3.Controls.Add(this.chk_fc);
            this.gp_box3.Controls.Add(this.chk_alliance);
            this.gp_box3.Controls.Add(this.chk_ls7);
            this.gp_box3.Controls.Add(this.chk_party);
            this.gp_box3.Controls.Add(this.chk_yell);
            this.gp_box3.Controls.Add(this.chk_ls6);
            this.gp_box3.Controls.Add(this.chk_shout);
            this.gp_box3.Controls.Add(this.chk_tell);
            this.gp_box3.Controls.Add(this.chk_ls5);
            this.gp_box3.Controls.Add(this.chk_say);
            this.gp_box3.Controls.Add(this.chk_ls4);
            this.gp_box3.Controls.Add(this.chk_ls1);
            this.gp_box3.Controls.Add(this.chk_ls3);
            this.gp_box3.Controls.Add(this.chk_ls2);
            this.gp_box3.Location = new System.Drawing.Point(13, 347);
            this.gp_box3.Name = "gp_box3";
            this.gp_box3.Size = new System.Drawing.Size(860, 238);
            this.gp_box3.TabIndex = 8;
            this.gp_box3.TabStop = false;
            this.gp_box3.Text = "Chat Alerts";
            // 
            // btn_ls8Col
            // 
            this.btn_ls8Col.BackColor = System.Drawing.Color.White;
            this.btn_ls8Col.ForeColor = System.Drawing.Color.White;
            this.btn_ls8Col.Location = new System.Drawing.Point(717, 188);
            this.btn_ls8Col.Name = "btn_ls8Col";
            this.btn_ls8Col.Size = new System.Drawing.Size(59, 23);
            this.btn_ls8Col.TabIndex = 42;
            this.btn_ls8Col.UseVisualStyleBackColor = false;
            this.btn_ls8Col.Click += new System.EventHandler(this.btn_ls8Col_Click);
            // 
            // btn_ls7Col
            // 
            this.btn_ls7Col.BackColor = System.Drawing.Color.White;
            this.btn_ls7Col.ForeColor = System.Drawing.Color.White;
            this.btn_ls7Col.Location = new System.Drawing.Point(717, 148);
            this.btn_ls7Col.Name = "btn_ls7Col";
            this.btn_ls7Col.Size = new System.Drawing.Size(59, 23);
            this.btn_ls7Col.TabIndex = 41;
            this.btn_ls7Col.UseVisualStyleBackColor = false;
            this.btn_ls7Col.Click += new System.EventHandler(this.btn_ls7Col_Click);
            // 
            // btn_ls6Col
            // 
            this.btn_ls6Col.BackColor = System.Drawing.Color.White;
            this.btn_ls6Col.ForeColor = System.Drawing.Color.White;
            this.btn_ls6Col.Location = new System.Drawing.Point(717, 108);
            this.btn_ls6Col.Name = "btn_ls6Col";
            this.btn_ls6Col.Size = new System.Drawing.Size(59, 23);
            this.btn_ls6Col.TabIndex = 40;
            this.btn_ls6Col.UseVisualStyleBackColor = false;
            this.btn_ls6Col.Click += new System.EventHandler(this.btn_ls6Col_Click);
            // 
            // btn_ls5Col
            // 
            this.btn_ls5Col.BackColor = System.Drawing.Color.White;
            this.btn_ls5Col.ForeColor = System.Drawing.Color.White;
            this.btn_ls5Col.Location = new System.Drawing.Point(717, 68);
            this.btn_ls5Col.Name = "btn_ls5Col";
            this.btn_ls5Col.Size = new System.Drawing.Size(59, 23);
            this.btn_ls5Col.TabIndex = 39;
            this.btn_ls5Col.UseVisualStyleBackColor = false;
            this.btn_ls5Col.Click += new System.EventHandler(this.btn_ls5Col_Click);
            // 
            // btn_ls4Col
            // 
            this.btn_ls4Col.BackColor = System.Drawing.Color.White;
            this.btn_ls4Col.ForeColor = System.Drawing.Color.White;
            this.btn_ls4Col.Location = new System.Drawing.Point(717, 28);
            this.btn_ls4Col.Name = "btn_ls4Col";
            this.btn_ls4Col.Size = new System.Drawing.Size(59, 23);
            this.btn_ls4Col.TabIndex = 38;
            this.btn_ls4Col.UseVisualStyleBackColor = false;
            this.btn_ls4Col.Click += new System.EventHandler(this.btn_ls4Col_Click);
            // 
            // btn_ls3Col
            // 
            this.btn_ls3Col.BackColor = System.Drawing.Color.White;
            this.btn_ls3Col.ForeColor = System.Drawing.Color.White;
            this.btn_ls3Col.Location = new System.Drawing.Point(471, 187);
            this.btn_ls3Col.Name = "btn_ls3Col";
            this.btn_ls3Col.Size = new System.Drawing.Size(59, 23);
            this.btn_ls3Col.TabIndex = 37;
            this.btn_ls3Col.UseVisualStyleBackColor = false;
            this.btn_ls3Col.Click += new System.EventHandler(this.btn_ls3Col_Click);
            // 
            // btn_ls2Col
            // 
            this.btn_ls2Col.BackColor = System.Drawing.Color.White;
            this.btn_ls2Col.ForeColor = System.Drawing.Color.White;
            this.btn_ls2Col.Location = new System.Drawing.Point(471, 147);
            this.btn_ls2Col.Name = "btn_ls2Col";
            this.btn_ls2Col.Size = new System.Drawing.Size(59, 23);
            this.btn_ls2Col.TabIndex = 36;
            this.btn_ls2Col.UseVisualStyleBackColor = false;
            this.btn_ls2Col.Click += new System.EventHandler(this.btn_ls2Col_Click);
            // 
            // btn_ls1Col
            // 
            this.btn_ls1Col.BackColor = System.Drawing.Color.White;
            this.btn_ls1Col.ForeColor = System.Drawing.Color.White;
            this.btn_ls1Col.Location = new System.Drawing.Point(471, 107);
            this.btn_ls1Col.Name = "btn_ls1Col";
            this.btn_ls1Col.Size = new System.Drawing.Size(59, 23);
            this.btn_ls1Col.TabIndex = 35;
            this.btn_ls1Col.UseVisualStyleBackColor = false;
            this.btn_ls1Col.Click += new System.EventHandler(this.btn_ls1Col_Click);
            // 
            // btn_fcCol
            // 
            this.btn_fcCol.BackColor = System.Drawing.Color.White;
            this.btn_fcCol.ForeColor = System.Drawing.Color.White;
            this.btn_fcCol.Location = new System.Drawing.Point(471, 67);
            this.btn_fcCol.Name = "btn_fcCol";
            this.btn_fcCol.Size = new System.Drawing.Size(59, 23);
            this.btn_fcCol.TabIndex = 34;
            this.btn_fcCol.UseVisualStyleBackColor = false;
            this.btn_fcCol.Click += new System.EventHandler(this.btn_fcCol_Click);
            // 
            // btn_allianceCol
            // 
            this.btn_allianceCol.BackColor = System.Drawing.Color.White;
            this.btn_allianceCol.ForeColor = System.Drawing.Color.White;
            this.btn_allianceCol.Location = new System.Drawing.Point(471, 27);
            this.btn_allianceCol.Name = "btn_allianceCol";
            this.btn_allianceCol.Size = new System.Drawing.Size(59, 23);
            this.btn_allianceCol.TabIndex = 33;
            this.btn_allianceCol.UseVisualStyleBackColor = false;
            this.btn_allianceCol.Click += new System.EventHandler(this.btn_allianceCol_Click);
            // 
            // btn_partyCol
            // 
            this.btn_partyCol.BackColor = System.Drawing.Color.White;
            this.btn_partyCol.ForeColor = System.Drawing.Color.White;
            this.btn_partyCol.Location = new System.Drawing.Point(192, 187);
            this.btn_partyCol.Margin = new System.Windows.Forms.Padding(3, 3, 15, 3);
            this.btn_partyCol.Name = "btn_partyCol";
            this.btn_partyCol.Size = new System.Drawing.Size(59, 23);
            this.btn_partyCol.TabIndex = 32;
            this.btn_partyCol.UseVisualStyleBackColor = false;
            this.btn_partyCol.Click += new System.EventHandler(this.btn_partyCol_Click);
            // 
            // btn_yellCol
            // 
            this.btn_yellCol.BackColor = System.Drawing.Color.White;
            this.btn_yellCol.ForeColor = System.Drawing.Color.White;
            this.btn_yellCol.Location = new System.Drawing.Point(192, 147);
            this.btn_yellCol.Margin = new System.Windows.Forms.Padding(3, 3, 15, 3);
            this.btn_yellCol.Name = "btn_yellCol";
            this.btn_yellCol.Size = new System.Drawing.Size(59, 23);
            this.btn_yellCol.TabIndex = 31;
            this.btn_yellCol.UseVisualStyleBackColor = false;
            this.btn_yellCol.Click += new System.EventHandler(this.btn_yellCol_Click);
            // 
            // btn_shoutCol
            // 
            this.btn_shoutCol.BackColor = System.Drawing.Color.White;
            this.btn_shoutCol.ForeColor = System.Drawing.Color.White;
            this.btn_shoutCol.Location = new System.Drawing.Point(192, 107);
            this.btn_shoutCol.Margin = new System.Windows.Forms.Padding(3, 3, 15, 3);
            this.btn_shoutCol.Name = "btn_shoutCol";
            this.btn_shoutCol.Size = new System.Drawing.Size(59, 23);
            this.btn_shoutCol.TabIndex = 30;
            this.btn_shoutCol.UseVisualStyleBackColor = false;
            this.btn_shoutCol.Click += new System.EventHandler(this.btn_shoutCol_Click);
            // 
            // btn_tellCol
            // 
            this.btn_tellCol.BackColor = System.Drawing.Color.White;
            this.btn_tellCol.ForeColor = System.Drawing.Color.White;
            this.btn_tellCol.Location = new System.Drawing.Point(192, 67);
            this.btn_tellCol.Margin = new System.Windows.Forms.Padding(3, 3, 15, 3);
            this.btn_tellCol.Name = "btn_tellCol";
            this.btn_tellCol.Size = new System.Drawing.Size(59, 23);
            this.btn_tellCol.TabIndex = 29;
            this.btn_tellCol.UseVisualStyleBackColor = false;
            this.btn_tellCol.Click += new System.EventHandler(this.btn_tellCol_Click);
            // 
            // btn_sayCol
            // 
            this.btn_sayCol.BackColor = System.Drawing.Color.White;
            this.btn_sayCol.ForeColor = System.Drawing.Color.White;
            this.btn_sayCol.Location = new System.Drawing.Point(192, 27);
            this.btn_sayCol.Margin = new System.Windows.Forms.Padding(3, 3, 15, 3);
            this.btn_sayCol.Name = "btn_sayCol";
            this.btn_sayCol.Size = new System.Drawing.Size(59, 23);
            this.btn_sayCol.TabIndex = 14;
            this.btn_sayCol.UseVisualStyleBackColor = false;
            this.btn_sayCol.Click += new System.EventHandler(this.btn_sayCol_Click);
            // 
            // chk_ls8
            // 
            this.chk_ls8.AutoSize = true;
            this.chk_ls8.Location = new System.Drawing.Point(605, 192);
            this.chk_ls8.Name = "chk_ls8";
            this.chk_ls8.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_ls8.Size = new System.Drawing.Size(106, 17);
            this.chk_ls8.TabIndex = 28;
            this.chk_ls8.Text = "Linkshell 8";
            this.chk_ls8.UseVisualStyleBackColor = true;
            this.chk_ls8.CheckedChanged += new System.EventHandler(this.chk_ls8_CheckedChanged);
            // 
            // chk_fc
            // 
            this.chk_fc.AutoSize = true;
            this.chk_fc.Location = new System.Drawing.Point(336, 71);
            this.chk_fc.Name = "chk_fc";
            this.chk_fc.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_fc.Size = new System.Drawing.Size(124, 17);
            this.chk_fc.TabIndex = 20;
            this.chk_fc.Text = "Free Company";
            this.chk_fc.UseVisualStyleBackColor = true;
            this.chk_fc.CheckedChanged += new System.EventHandler(this.chk_fc_CheckedChanged);
            // 
            // chk_alliance
            // 
            this.chk_alliance.AutoSize = true;
            this.chk_alliance.Location = new System.Drawing.Point(336, 31);
            this.chk_alliance.Name = "chk_alliance";
            this.chk_alliance.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_alliance.Size = new System.Drawing.Size(93, 17);
            this.chk_alliance.TabIndex = 19;
            this.chk_alliance.Text = "Alliance";
            this.chk_alliance.UseVisualStyleBackColor = true;
            this.chk_alliance.CheckedChanged += new System.EventHandler(this.chk_alliance_CheckedChanged);
            // 
            // chk_ls7
            // 
            this.chk_ls7.AutoSize = true;
            this.chk_ls7.Location = new System.Drawing.Point(605, 152);
            this.chk_ls7.Name = "chk_ls7";
            this.chk_ls7.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_ls7.Size = new System.Drawing.Size(106, 17);
            this.chk_ls7.TabIndex = 27;
            this.chk_ls7.Text = "Linkshell 7";
            this.chk_ls7.UseVisualStyleBackColor = true;
            this.chk_ls7.CheckedChanged += new System.EventHandler(this.chk_ls7_CheckedChanged);
            // 
            // chk_party
            // 
            this.chk_party.AutoSize = true;
            this.chk_party.Location = new System.Drawing.Point(84, 191);
            this.chk_party.Name = "chk_party";
            this.chk_party.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_party.Size = new System.Drawing.Size(80, 17);
            this.chk_party.TabIndex = 18;
            this.chk_party.Text = "Party";
            this.chk_party.UseVisualStyleBackColor = true;
            this.chk_party.CheckedChanged += new System.EventHandler(this.chk_party_CheckedChanged);
            // 
            // chk_yell
            // 
            this.chk_yell.AutoSize = true;
            this.chk_yell.Location = new System.Drawing.Point(84, 151);
            this.chk_yell.Name = "chk_yell";
            this.chk_yell.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_yell.Size = new System.Drawing.Size(73, 17);
            this.chk_yell.TabIndex = 17;
            this.chk_yell.Text = "Yell";
            this.chk_yell.UseVisualStyleBackColor = true;
            this.chk_yell.CheckedChanged += new System.EventHandler(this.chk_yell_CheckedChanged);
            // 
            // chk_ls6
            // 
            this.chk_ls6.AutoSize = true;
            this.chk_ls6.Location = new System.Drawing.Point(605, 112);
            this.chk_ls6.Name = "chk_ls6";
            this.chk_ls6.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_ls6.Size = new System.Drawing.Size(106, 17);
            this.chk_ls6.TabIndex = 26;
            this.chk_ls6.Text = "Linkshell 6";
            this.chk_ls6.UseVisualStyleBackColor = true;
            this.chk_ls6.CheckedChanged += new System.EventHandler(this.chk_ls6_CheckedChanged);
            // 
            // chk_shout
            // 
            this.chk_shout.AutoSize = true;
            this.chk_shout.Location = new System.Drawing.Point(84, 111);
            this.chk_shout.Name = "chk_shout";
            this.chk_shout.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_shout.Size = new System.Drawing.Size(84, 17);
            this.chk_shout.TabIndex = 16;
            this.chk_shout.Text = "Shout";
            this.chk_shout.UseVisualStyleBackColor = true;
            this.chk_shout.CheckedChanged += new System.EventHandler(this.chk_shout_CheckedChanged);
            // 
            // chk_tell
            // 
            this.chk_tell.AutoSize = true;
            this.chk_tell.Location = new System.Drawing.Point(84, 71);
            this.chk_tell.Name = "chk_tell";
            this.chk_tell.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_tell.Size = new System.Drawing.Size(73, 17);
            this.chk_tell.TabIndex = 15;
            this.chk_tell.Text = "Tell";
            this.chk_tell.UseVisualStyleBackColor = true;
            this.chk_tell.CheckedChanged += new System.EventHandler(this.chk_tell_CheckedChanged);
            // 
            // chk_ls5
            // 
            this.chk_ls5.AutoSize = true;
            this.chk_ls5.Location = new System.Drawing.Point(605, 72);
            this.chk_ls5.Name = "chk_ls5";
            this.chk_ls5.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_ls5.Size = new System.Drawing.Size(106, 17);
            this.chk_ls5.TabIndex = 25;
            this.chk_ls5.Text = "Linkshell 5";
            this.chk_ls5.UseVisualStyleBackColor = true;
            this.chk_ls5.CheckedChanged += new System.EventHandler(this.chk_ls5_CheckedChanged);
            // 
            // chk_say
            // 
            this.chk_say.AutoSize = true;
            this.chk_say.Location = new System.Drawing.Point(84, 31);
            this.chk_say.Name = "chk_say";
            this.chk_say.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_say.Size = new System.Drawing.Size(74, 17);
            this.chk_say.TabIndex = 14;
            this.chk_say.Text = "Say";
            this.chk_say.UseVisualStyleBackColor = true;
            this.chk_say.CheckedChanged += new System.EventHandler(this.chk_say_CheckedChanged);
            // 
            // chk_ls4
            // 
            this.chk_ls4.AutoSize = true;
            this.chk_ls4.Location = new System.Drawing.Point(605, 32);
            this.chk_ls4.Name = "chk_ls4";
            this.chk_ls4.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_ls4.Size = new System.Drawing.Size(106, 17);
            this.chk_ls4.TabIndex = 24;
            this.chk_ls4.Text = "Linkshell 4";
            this.chk_ls4.UseVisualStyleBackColor = true;
            this.chk_ls4.CheckedChanged += new System.EventHandler(this.chk_ls4_CheckedChanged);
            // 
            // chk_ls1
            // 
            this.chk_ls1.AutoSize = true;
            this.chk_ls1.Location = new System.Drawing.Point(336, 111);
            this.chk_ls1.Name = "chk_ls1";
            this.chk_ls1.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_ls1.Size = new System.Drawing.Size(106, 17);
            this.chk_ls1.TabIndex = 21;
            this.chk_ls1.Text = "Linkshell 1";
            this.chk_ls1.UseVisualStyleBackColor = true;
            this.chk_ls1.CheckedChanged += new System.EventHandler(this.chk_ls1_CheckedChanged);
            // 
            // chk_ls3
            // 
            this.chk_ls3.AutoSize = true;
            this.chk_ls3.Location = new System.Drawing.Point(336, 191);
            this.chk_ls3.Name = "chk_ls3";
            this.chk_ls3.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_ls3.Size = new System.Drawing.Size(106, 17);
            this.chk_ls3.TabIndex = 23;
            this.chk_ls3.Text = "Linkshell 3";
            this.chk_ls3.UseVisualStyleBackColor = true;
            this.chk_ls3.CheckedChanged += new System.EventHandler(this.chk_ls3_CheckedChanged);
            // 
            // chk_ls2
            // 
            this.chk_ls2.AutoSize = true;
            this.chk_ls2.Location = new System.Drawing.Point(336, 151);
            this.chk_ls2.Name = "chk_ls2";
            this.chk_ls2.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            this.chk_ls2.Size = new System.Drawing.Size(106, 17);
            this.chk_ls2.TabIndex = 22;
            this.chk_ls2.Text = "Linkshell 2";
            this.chk_ls2.UseVisualStyleBackColor = true;
            this.chk_ls2.CheckedChanged += new System.EventHandler(this.chk_ls2_CheckedChanged);
            // 
            // dia_say
            // 
            this.dia_say.AnyColor = true;
            this.dia_say.FullOpen = true;
            // 
            // dia_tell
            // 
            this.dia_tell.AnyColor = true;
            this.dia_tell.FullOpen = true;
            // 
            // dia_shout
            // 
            this.dia_shout.AnyColor = true;
            this.dia_shout.FullOpen = true;
            // 
            // dia_yell
            // 
            this.dia_yell.AnyColor = true;
            this.dia_yell.FullOpen = true;
            // 
            // dia_party
            // 
            this.dia_party.AnyColor = true;
            this.dia_party.FullOpen = true;
            // 
            // dia_alliance
            // 
            this.dia_alliance.AnyColor = true;
            this.dia_alliance.FullOpen = true;
            // 
            // dia_ls1
            // 
            this.dia_ls1.AnyColor = true;
            this.dia_ls1.FullOpen = true;
            // 
            // dia_ls2
            // 
            this.dia_ls2.AnyColor = true;
            this.dia_ls2.FullOpen = true;
            // 
            // dia_ls3
            // 
            this.dia_ls3.AnyColor = true;
            this.dia_ls3.FullOpen = true;
            // 
            // dia_ls4
            // 
            this.dia_ls4.AnyColor = true;
            this.dia_ls4.FullOpen = true;
            // 
            // dia_ls5
            // 
            this.dia_ls5.AnyColor = true;
            this.dia_ls5.FullOpen = true;
            // 
            // dia_ls6
            // 
            this.dia_ls6.AnyColor = true;
            this.dia_ls6.FullOpen = true;
            // 
            // dia_ls7
            // 
            this.dia_ls7.AnyColor = true;
            this.dia_ls7.FullOpen = true;
            // 
            // dia_ls8
            // 
            this.dia_ls8.AnyColor = true;
            this.dia_ls8.FullOpen = true;
            // 
            // dia_fc
            // 
            this.dia_fc.AnyColor = true;
            this.dia_fc.FullOpen = true;
            // 
            // gp_box4
            // 
            this.gp_box4.Controls.Add(this.chk_reactiveWeather);
            this.gp_box4.Controls.Add(this.btn_restoreDefaults);
            this.gp_box4.Controls.Add(this.btn_raidEffectsB);
            this.gp_box4.Controls.Add(this.btn_raidEffectsA);
            this.gp_box4.Controls.Add(this.chk_raidEffects);
            this.gp_box4.Controls.Add(this.chk_GoldSaucerVegas);
            this.gp_box4.Location = new System.Drawing.Point(13, 597);
            this.gp_box4.Name = "gp_box4";
            this.gp_box4.Size = new System.Drawing.Size(860, 100);
            this.gp_box4.TabIndex = 9;
            this.gp_box4.TabStop = false;
            this.gp_box4.Text = "Special";
            // 
            // chk_reactiveWeather
            // 
            this.chk_reactiveWeather.AutoSize = true;
            this.chk_reactiveWeather.Location = new System.Drawing.Point(551, 42);
            this.chk_reactiveWeather.Name = "chk_reactiveWeather";
            this.chk_reactiveWeather.Size = new System.Drawing.Size(113, 17);
            this.chk_reactiveWeather.TabIndex = 18;
            this.chk_reactiveWeather.Text = "Reactive Weather";
            this.chk_reactiveWeather.UseVisualStyleBackColor = true;
            this.chk_reactiveWeather.CheckedChanged += new System.EventHandler(this.chk_reactiveWeather_CheckedChanged);
            // 
            // btn_restoreDefaults
            // 
            this.btn_restoreDefaults.Location = new System.Drawing.Point(716, 35);
            this.btn_restoreDefaults.Name = "btn_restoreDefaults";
            this.btn_restoreDefaults.Size = new System.Drawing.Size(105, 31);
            this.btn_restoreDefaults.TabIndex = 17;
            this.btn_restoreDefaults.Text = "Restore Defaults";
            this.btn_restoreDefaults.UseVisualStyleBackColor = true;
            this.btn_restoreDefaults.Click += new System.EventHandler(this.btn_restoreDefaults_Click);
            // 
            // btn_raidEffectsB
            // 
            this.btn_raidEffectsB.BackColor = System.Drawing.Color.Gold;
            this.btn_raidEffectsB.ForeColor = System.Drawing.Color.Gold;
            this.btn_raidEffectsB.Location = new System.Drawing.Point(426, 38);
            this.btn_raidEffectsB.Margin = new System.Windows.Forms.Padding(3, 3, 15, 3);
            this.btn_raidEffectsB.Name = "btn_raidEffectsB";
            this.btn_raidEffectsB.Size = new System.Drawing.Size(59, 23);
            this.btn_raidEffectsB.TabIndex = 16;
            this.btn_raidEffectsB.UseVisualStyleBackColor = false;
            this.btn_raidEffectsB.Click += new System.EventHandler(this.btn_raidEffectsB_Click);
            // 
            // btn_raidEffectsA
            // 
            this.btn_raidEffectsA.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.btn_raidEffectsA.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.btn_raidEffectsA.Location = new System.Drawing.Point(354, 38);
            this.btn_raidEffectsA.Margin = new System.Windows.Forms.Padding(3, 3, 15, 3);
            this.btn_raidEffectsA.Name = "btn_raidEffectsA";
            this.btn_raidEffectsA.Size = new System.Drawing.Size(59, 23);
            this.btn_raidEffectsA.TabIndex = 15;
            this.btn_raidEffectsA.UseVisualStyleBackColor = false;
            this.btn_raidEffectsA.Click += new System.EventHandler(this.btn_raidEffectsA_Click);
            // 
            // chk_raidEffects
            // 
            this.chk_raidEffects.AutoSize = true;
            this.chk_raidEffects.Location = new System.Drawing.Point(264, 42);
            this.chk_raidEffects.Name = "chk_raidEffects";
            this.chk_raidEffects.Size = new System.Drawing.Size(84, 17);
            this.chk_raidEffects.TabIndex = 1;
            this.chk_raidEffects.Text = "Raid Effects";
            this.chk_raidEffects.UseVisualStyleBackColor = true;
            this.chk_raidEffects.CheckedChanged += new System.EventHandler(this.chk_raidEffects_CheckChanged);
            // 
            // chk_GoldSaucerVegas
            // 
            this.chk_GoldSaucerVegas.AutoSize = true;
            this.chk_GoldSaucerVegas.Location = new System.Drawing.Point(40, 42);
            this.chk_GoldSaucerVegas.Name = "chk_GoldSaucerVegas";
            this.chk_GoldSaucerVegas.Size = new System.Drawing.Size(159, 17);
            this.chk_GoldSaucerVegas.TabIndex = 0;
            this.chk_GoldSaucerVegas.Text = "Vegas Mode in Gold Saucer";
            this.chk_GoldSaucerVegas.UseVisualStyleBackColor = true;
            this.chk_GoldSaucerVegas.CheckedChanged += new System.EventHandler(this.chk_GoldSaucerVegas_CheckedChanged);
            // 
            // dia_raidA
            // 
            this.dia_raidA.AnyColor = true;
            this.dia_raidA.FullOpen = true;
            // 
            // dia_raidB
            // 
            this.dia_raidB.AnyColor = true;
            this.dia_raidB.FullOpen = true;
            // 
            // dia_col4
            // 
            this.dia_col4.AnyColor = true;
            this.dia_col4.FullOpen = true;
            // 
            // pic_Logo
            // 
            this.pic_Logo.Location = new System.Drawing.Point(713, 703);
            this.pic_Logo.Name = "pic_Logo";
            this.pic_Logo.Size = new System.Drawing.Size(160, 64);
            this.pic_Logo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pic_Logo.TabIndex = 10;
            this.pic_Logo.TabStop = false;
            this.pic_Logo.Click += new System.EventHandler(this.pic_Logo_Click);
            // 
            // dia_DPSlimit
            // 
            this.dia_DPSlimit.AnyColor = true;
            this.dia_DPSlimit.FullOpen = true;
            // 
            // Chromatics
            // 
            this.AutoScroll = true;
            this.Controls.Add(this.pic_Logo);
            this.Controls.Add(this.gp_box4);
            this.Controls.Add(this.gp_box3);
            this.Controls.Add(this.gp_box2);
            this.Controls.Add(this.gp_box1);
            this.Name = "Chromatics";
            this.Size = new System.Drawing.Size(895, 851);
            this.gp_box1.ResumeLayout(false);
            this.gp_box1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txt_DPSNotify)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txt_DPSlimit)).EndInit();
            this.gp_box2.ResumeLayout(false);
            this.gp_box2.PerformLayout();
            this.gp_box3.ResumeLayout(false);
            this.gp_box3.PerformLayout();
            this.gp_box4.ResumeLayout(false);
            this.gp_box4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pic_Logo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private CheckBox chk_enableEmnity;
        private CheckBox chk_enableTriggers;
        private Label lbl_defaultCol;
        private Label lbl_emnityCol;
        private GroupBox gp_box1;
        private ColorDialog dia_col1;
        private Button btn_emnityCol;
        private ColorDialog dia_col2;
        private GroupBox gp_box2;
        private CheckBox chk_deviceKeypad;
        private CheckBox chk_deviceKeyboard;
        private CheckBox chk_deviceMousepad;
        private CheckBox chk_deviceHeadset;
        private CheckBox chk_deviceMouse;
        private Button btn_rescan;
        private Button btn_triggerCol;
        private Label lbl_trigCol;
        private ColorDialog dia_col3;
        private Button btn_defaultCol;

        #endregion

        private Label lbl_triggerCount;
        private ComboBox cb_TriggerCount;
        private Label lbl_triggerSpeed;
        private GroupBox gp_box3;
        private CheckBox chk_say;
        private CheckBox chk_ls7;
        private CheckBox chk_ls6;
        private CheckBox chk_ls5;
        private CheckBox chk_ls4;
        private CheckBox chk_ls3;
        private CheckBox chk_ls2;
        private CheckBox chk_ls1;
        private CheckBox chk_fc;
        private CheckBox chk_alliance;
        private CheckBox chk_party;
        private CheckBox chk_yell;
        private CheckBox chk_shout;
        private CheckBox chk_tell;
        private CheckBox chk_ls8;
        private Button btn_ls8Col;
        private Button btn_ls7Col;
        private Button btn_ls6Col;
        private Button btn_ls5Col;
        private Button btn_ls4Col;
        private Button btn_ls3Col;
        private Button btn_ls2Col;
        private Button btn_ls1Col;
        private Button btn_fcCol;
        private Button btn_allianceCol;
        private Button btn_partyCol;
        private Button btn_yellCol;
        private Button btn_shoutCol;
        private Button btn_tellCol;
        private Button btn_sayCol;
        private ColorDialog dia_say;
        private ColorDialog dia_tell;
        private ColorDialog dia_shout;
        private ColorDialog dia_yell;
        private ColorDialog dia_party;
        private ColorDialog dia_alliance;
        private ColorDialog dia_ls1;
        private ColorDialog dia_ls2;
        private ColorDialog dia_ls3;
        private ColorDialog dia_ls4;
        private ColorDialog dia_ls5;
        private ColorDialog dia_ls6;
        private ColorDialog dia_ls7;
        private ColorDialog dia_ls8;
        private ColorDialog dia_fc;
        private GroupBox gp_box4;
        private CheckBox chk_GoldSaucerVegas;
        private Button btn_raidEffectsA;
        private CheckBox chk_raidEffects;
        private Button btn_raidEffectsB;
        private ColorDialog dia_raidA;
        private ColorDialog dia_raidB;
        private CheckBox chk_deviceLogitech;
        private Label lbl_TimerEvent;
        private ComboBox cb_Event;
        private Label lbl_TimerCount;
        private ComboBox cb_TimerCount;
        private Button btn_timerCol;
        private Label lbl_TimerCol;
        private CheckBox chk_enableTimers;
        private ColorDialog dia_col4;
        private ComboBox cb_TriggerSpeed;
        private Button btn_restoreDefaults;
        private PictureBox pic_Logo;
        private CheckBox chk_reactiveWeather;
        private Label lbl_dpsMin;
        private NumericUpDown txt_DPSlimit;
        private Button btn_DPSLimitCol;
        private Label lbl_alertCol;
        private CheckBox chk_DPSLimit;
        private ColorDialog dia_DPSlimit;
        private Label lbl_NotifyDPS;
        private NumericUpDown txt_DPSNotify;
        private CheckBox chk_deviceCorsair;
        Label lblStatus;


        public Chromatics()
        {
            //reserved
        }

        //Setup Globals

        private bool CorsairSDK = false;
        private bool LogitechSDK = false;
        private bool StartUp = false;
        private bool rescan = false;
        private bool CorsairKeyboardDetect = false;
        private bool CorsairMouseDetect = false;
        private bool CorsairHeadsetDetect = false;
        private bool RzPulse = false;
        private bool DPSparse = false;
        private bool ChromaReady = false;
        private bool Trigger = false;
        private bool DeviceKeyboard = false;
        private bool DeviceKeypad = false;
        private bool DeviceMouse = false;
        private bool DeviceMousepad = false;
        private bool DeviceHeadset = false;
        private bool DeviceLogitech = false;
        private bool RazerSDK = false;
        private bool inCombat = false;
        private bool LogiEffectRunning = false;
        private int state = 0;
        private int maxtick = 6;
        private int timercount = 6;
        private int tickspeed = 300;
        private string currentZone;
        private string currentWeather;
        private string timerevent = "Expire";
        private string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string pluginEnviroment;
        string settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Config\\Chromatics.config.xml");
        private EncounterData ActiveEnconter;
        CorsairKeyboard corKeyboard;
        SettingsSerializer xmlSettings;
        System.Timers.Timer skyWatcher;
        List<string> customTriggers = new List<string>();
        List<string> raidZoneList = new List<string>();


        //Init Advance Combat Tracker & attach as plugin

        #region IActPluginV1 Members
        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            ActPluginData pluginData = ActGlobals.oFormActMain.PluginGetSelfData(this);
            pluginEnviroment = Path.GetDirectoryName(pluginData.pluginFile.ToString());

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler((sender, e) => LoadFromSameFolder(sender, e, pluginEnviroment));

            if (File.Exists(Environment.GetEnvironmentVariable("ProgramFiles") + @"\Razer Chroma SDK\bin\RzChromaSDK64.dll"))
            {
                RazerSDK = true;
                InitializeComponent();
                InitializeDevices();
            }
            else
            {
                RazerSDK = false;
                InitializeComponent();
                InitializeDevices();
            }

            lblStatus = pluginStatusText;
            pluginScreenSpace.Text = "Chromatics";
            pluginScreenSpace.Controls.Add(this);
            this.Dock = DockStyle.Fill;
            xmlSettings = new SettingsSerializer(this);
            var cTriggers = ActGlobals.oFormActMain.ActiveCustomTriggers;
            pic_Logo.Image = Image.FromFile(pluginEnviroment + "\\lib\\logo.png");
            LoadSettings();

            // Create parsing event handlers.  After the "+=" hit TAB twice and the code will be generated for you.
            ActGlobals.oFormActMain.AfterCombatAction += new CombatActionDelegate(oFormActMain_AfterCombatAction);
            ActGlobals.oFormActMain.OnCombatStart += new CombatToggleEventDelegate(oFormActMain_OnCombatStart);
            ActGlobals.oFormActMain.OnCombatEnd += new CombatToggleEventDelegate(oFormActMain_OnCombatEnd);
            ActGlobals.oFormActMain.BeforeLogLineRead += new LogLineEventDelegate(oFormActMain_BeforeLogLineRead); //READ
            ActGlobals.oFormSpellTimers.OnSpellTimerExpire += new SpellTimerEventDelegate(oFormSpellTimers_OnSpellTimerExpire);
            ActGlobals.oFormSpellTimers.OnSpellTimerWarning += new SpellTimerEventDelegate(oFormSpellTimers_OnSpellTimerWarning);
            ActGlobals.oFormSpellTimers.OnSpellTimerRemoved += new SpellTimerEventDelegate(oFormSpellTimers_OnSpellTimerRemoved);
            Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
            ActGlobals.oFormActMain.UpdateCheckClicked += new FormActMain.NullDelegate(oFormActMain_UpdateCheckClicked);

            //Start Automatic update listener on init
            if (ActGlobals.oFormActMain.GetAutomaticUpdatesAllowed())
            {
                new Thread(new ThreadStart(oFormActMain_UpdateCheckClicked)).Start();
            }
            
            //Fetch custom triggers from ACT and store in array
            foreach (var cTrigger in cTriggers)
            {
                var preparse = cTrigger.Key;
                string[] preparsearray = preparse.Split(new string[] { "|" }, StringSplitOptions.None);
                customTriggers.Add(preparsearray[1]);
            }

            //Setup Weather timer
            skyWatcher = new System.Timers.Timer();
            skyWatcher.Elapsed += (source, e) => { weatherLoop(); };
            skyWatcher.Interval = 10000;
            skyWatcher.Enabled = false;

            if (chk_reactiveWeather.Checked)
            {
                skyWatcher.Enabled = true;
            }

            //Setup Raid Zones
            raidZoneList.Add("Lower Aetheroacoustic Exploratory Site");
            raidZoneList.Add("Upper Aetheroacoustic Exploratory Site");
            raidZoneList.Add("The Ragnarok");
            raidZoneList.Add("Ragnarok Drive Cylinder");
            raidZoneList.Add("Ragnarok Central Core");
            raidZoneList.Add("Dalamud's Shadow");
            raidZoneList.Add("The Outer Coil");
            raidZoneList.Add("Central Decks");
            raidZoneList.Add("The Holocharts");
            raidZoneList.Add("IC-06 Central Decks");
            raidZoneList.Add("IC-06 Regeneration Grid");
            raidZoneList.Add("IC-06 Main Bridge");
            raidZoneList.Add("The Burning Heart");
            raidZoneList.Add("Labyrinth Of The Ancients");
            raidZoneList.Add("Syrcus Tower");
            raidZoneList.Add("The World Of Darkness");
            raidZoneList.Add("Fist Of The Father");
            raidZoneList.Add("Cuff Of The Father");
            raidZoneList.Add("Arm Of The Father");
            raidZoneList.Add("Burden Of The Father");
            raidZoneList.Add("Fist Of The Father (Savage)");
            raidZoneList.Add("Cuff Of The Father (Savage)");
            raidZoneList.Add("Arm Of The Father (Savage)");
            raidZoneList.Add("Burden Of The Father (Savage)");
            raidZoneList.Add("Void Ark");
            raidZoneList.Add("The Binding Coil Of Bahamut - Turn (1)");
            raidZoneList.Add("The Binding Coil Of Bahamut - Turn (2)");
            raidZoneList.Add("The Binding Coil Of Bahamut - Turn (3)");
            raidZoneList.Add("The Binding Coil Of Bahamut - Turn (4)");
            raidZoneList.Add("The Binding Coil Of Bahamut - Turn (5)");
            raidZoneList.Add("The Second Coil Of Bahamut - Turn (1)");
            raidZoneList.Add("The Second Coil Of Bahamut - Turn (2)");
            raidZoneList.Add("The Second Coil Of Bahamut - Turn (3)");
            raidZoneList.Add("The Second Coil Of Bahamut - Turn (4)");
            raidZoneList.Add("The Second Coil Of Bahamut - Turn (1)(Savage)");
            raidZoneList.Add("The Second Coil Of Bahamut - Turn (2)(Savage)");
            raidZoneList.Add("The Second Coil Of Bahamut - Turn (3)(Savage)");
            raidZoneList.Add("The Second Coil Of Bahamut - Turn (4)(Savage)");
            raidZoneList.Add("The Final Coil Of Bahamut - Turn (1)");
            raidZoneList.Add("The Final Coil Of Bahamut - Turn (2)");
            raidZoneList.Add("The Final Coil Of Bahamut - Turn (3)");
            raidZoneList.Add("The Final Coil Of Bahamut - Turn (4)");

            //Return positive attach
            lblStatus.Text = "Chromatics Plugin Started";
        }
        

        //Handle assembly redirects
        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args, string pluginEnviroment)
        {
            string fpo = pluginEnviroment + "\\lib\\";
            string assemblyPath = Path.Combine(fpo, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(assemblyPath) == false) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }

        //Handle plugin shutdown
        public void DeInitPlugin()
        {
            // Unsubscribe from any events when exiting!
            ActGlobals.oFormActMain.AfterCombatAction -= oFormActMain_AfterCombatAction;
            ActGlobals.oFormActMain.OnCombatStart -= new CombatToggleEventDelegate(oFormActMain_OnCombatStart);
            ActGlobals.oFormActMain.OnCombatEnd -= new CombatToggleEventDelegate(oFormActMain_OnCombatEnd);
            ActGlobals.oFormActMain.BeforeLogLineRead -= new LogLineEventDelegate(oFormActMain_BeforeLogLineRead); //READ
            ActGlobals.oFormSpellTimers.OnSpellTimerExpire -= new SpellTimerEventDelegate(oFormSpellTimers_OnSpellTimerExpire);
            ActGlobals.oFormSpellTimers.OnSpellTimerWarning -= new SpellTimerEventDelegate(oFormSpellTimers_OnSpellTimerWarning);
            ActGlobals.oFormSpellTimers.OnSpellTimerRemoved -= new SpellTimerEventDelegate(oFormSpellTimers_OnSpellTimerRemoved);
            ActGlobals.oFormActMain.UpdateCheckClicked -= oFormActMain_UpdateCheckClicked;
            Application.ApplicationExit -= new EventHandler(this.OnApplicationExit);

            skyWatcher.Enabled = false;
            SaveSettings();
            lblStatus.Text = "Chromatics Plugin Exited";
        }
        #endregion

        private void OnApplicationExit(object sender, EventArgs e)
        {
            skyWatcher.Enabled = false;
        }

        //Load settings from config file
        void LoadSettings()
        {
            xmlSettings.AddControlSetting(chk_enableEmnity.Name, chk_enableEmnity);
            xmlSettings.AddControlSetting(chk_enableTriggers.Name, chk_enableTriggers);
            xmlSettings.AddControlSetting(btn_defaultCol.Name, btn_defaultCol);
            xmlSettings.AddControlSetting(btn_emnityCol.Name, btn_emnityCol);
            xmlSettings.AddControlSetting(btn_triggerCol.Name, btn_triggerCol);
            xmlSettings.AddControlSetting(cb_TriggerCount.Name, cb_TriggerCount);
            xmlSettings.AddControlSetting(cb_TriggerSpeed.Name, cb_TriggerSpeed);
            xmlSettings.AddControlSetting(chk_tell.Name, chk_tell);
            xmlSettings.AddControlSetting(chk_yell.Name, chk_yell);
            xmlSettings.AddControlSetting(chk_shout.Name, chk_shout);
            xmlSettings.AddControlSetting(chk_say.Name, chk_say);
            xmlSettings.AddControlSetting(chk_party.Name, chk_party);
            xmlSettings.AddControlSetting(chk_alliance.Name, chk_alliance);
            xmlSettings.AddControlSetting(chk_fc.Name, chk_fc);
            xmlSettings.AddControlSetting(chk_ls1.Name, chk_ls1);
            xmlSettings.AddControlSetting(chk_ls2.Name, chk_ls2);
            xmlSettings.AddControlSetting(chk_ls3.Name, chk_ls3);
            xmlSettings.AddControlSetting(chk_ls4.Name, chk_ls4);
            xmlSettings.AddControlSetting(chk_ls5.Name, chk_ls5);
            xmlSettings.AddControlSetting(chk_ls6.Name, chk_ls6);
            xmlSettings.AddControlSetting(chk_ls7.Name, chk_ls7);
            xmlSettings.AddControlSetting(chk_ls8.Name, chk_ls8);
            xmlSettings.AddControlSetting(btn_tellCol.Name, btn_tellCol);
            xmlSettings.AddControlSetting(btn_yellCol.Name, btn_yellCol);
            xmlSettings.AddControlSetting(btn_shoutCol.Name, btn_shoutCol);
            xmlSettings.AddControlSetting(btn_sayCol.Name, btn_sayCol);
            xmlSettings.AddControlSetting(btn_partyCol.Name, btn_partyCol);
            xmlSettings.AddControlSetting(btn_allianceCol.Name, btn_allianceCol);
            xmlSettings.AddControlSetting(btn_fcCol.Name, btn_fcCol);
            xmlSettings.AddControlSetting(btn_ls1Col.Name, btn_ls1Col);
            xmlSettings.AddControlSetting(btn_ls2Col.Name, btn_ls2Col);
            xmlSettings.AddControlSetting(btn_ls3Col.Name, btn_ls3Col);
            xmlSettings.AddControlSetting(btn_ls4Col.Name, btn_ls4Col);
            xmlSettings.AddControlSetting(btn_ls5Col.Name, btn_ls5Col);
            xmlSettings.AddControlSetting(btn_ls6Col.Name, btn_ls6Col);
            xmlSettings.AddControlSetting(btn_ls7Col.Name, btn_ls7Col);
            xmlSettings.AddControlSetting(btn_ls8Col.Name, btn_ls8Col);
            xmlSettings.AddControlSetting(chk_GoldSaucerVegas.Name, chk_GoldSaucerVegas);
            xmlSettings.AddControlSetting(chk_raidEffects.Name, chk_raidEffects);
            xmlSettings.AddControlSetting(btn_raidEffectsA.Name, btn_raidEffectsA);
            xmlSettings.AddControlSetting(btn_raidEffectsB.Name, btn_raidEffectsB);
            xmlSettings.AddControlSetting(chk_enableTimers.Name, chk_enableTimers);
            xmlSettings.AddControlSetting(cb_TimerCount.Name, cb_TimerCount);
            xmlSettings.AddControlSetting(cb_Event.Name, cb_Event);
            xmlSettings.AddControlSetting(btn_timerCol.Name, btn_timerCol);
            xmlSettings.AddControlSetting(chk_reactiveWeather.Name, chk_reactiveWeather);
            xmlSettings.AddControlSetting(chk_DPSLimit.Name, chk_DPSLimit);
            xmlSettings.AddControlSetting(btn_DPSLimitCol.Name, btn_DPSLimitCol);
            xmlSettings.AddControlSetting(txt_DPSlimit.Name, txt_DPSlimit);
            xmlSettings.AddControlSetting(txt_DPSNotify.Name, txt_DPSNotify);
            
            if (File.Exists(settingsFile))
            {
                FileStream fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                XmlTextReader xReader = new XmlTextReader(fs);

                try
                {
                    while (xReader.Read())
                    {
                        if (xReader.NodeType == XmlNodeType.Element)
                        {
                            if (xReader.LocalName == "SettingsSerializer")
                            {
                                xmlSettings.ImportFromXml(xReader);
                                System.Drawing.Color _defaultCol = System.Drawing.ColorTranslator.FromHtml(btn_defaultCol.Text);
                                System.Drawing.Color _emnityCol = System.Drawing.ColorTranslator.FromHtml(btn_emnityCol.Text);
                                System.Drawing.Color _triggerCol = System.Drawing.ColorTranslator.FromHtml(btn_triggerCol.Text);
                                btn_defaultCol.BackColor = _defaultCol;
                                btn_defaultCol.ForeColor = _defaultCol;
                                btn_emnityCol.BackColor = _emnityCol;
                                btn_emnityCol.ForeColor = _emnityCol;
                                btn_triggerCol.BackColor = _triggerCol;
                                btn_triggerCol.ForeColor = _triggerCol;
                                System.Drawing.Color saycol = System.Drawing.ColorTranslator.FromHtml(btn_sayCol.Text);
                                btn_sayCol.BackColor = saycol;
                                btn_sayCol.ForeColor = saycol;
                                System.Drawing.Color tellcol = System.Drawing.ColorTranslator.FromHtml(btn_tellCol.Text);
                                btn_tellCol.BackColor = tellcol;
                                btn_tellCol.ForeColor = tellcol;
                                System.Drawing.Color yellcol = System.Drawing.ColorTranslator.FromHtml(btn_yellCol.Text);
                                btn_yellCol.BackColor = yellcol;
                                btn_yellCol.ForeColor = yellcol;
                                System.Drawing.Color shoutcol = System.Drawing.ColorTranslator.FromHtml(btn_shoutCol.Text);
                                btn_shoutCol.BackColor = shoutcol;
                                btn_shoutCol.ForeColor = shoutcol;
                                System.Drawing.Color partycol = System.Drawing.ColorTranslator.FromHtml(btn_partyCol.Text);
                                btn_partyCol.BackColor = partycol;
                                btn_partyCol.ForeColor = partycol;
                                System.Drawing.Color alliancecol = System.Drawing.ColorTranslator.FromHtml(btn_allianceCol.Text);
                                btn_allianceCol.BackColor = alliancecol;
                                btn_allianceCol.ForeColor = alliancecol;
                                System.Drawing.Color fccol = System.Drawing.ColorTranslator.FromHtml(btn_fcCol.Text);
                                btn_fcCol.BackColor = fccol;
                                btn_fcCol.ForeColor = fccol;
                                System.Drawing.Color ls1col = System.Drawing.ColorTranslator.FromHtml(btn_ls1Col.Text);
                                btn_ls1Col.BackColor = ls1col;
                                btn_ls1Col.ForeColor = ls1col;
                                System.Drawing.Color ls2col = System.Drawing.ColorTranslator.FromHtml(btn_ls2Col.Text);
                                btn_ls2Col.BackColor = ls2col;
                                btn_ls2Col.ForeColor = ls2col;
                                System.Drawing.Color ls3col = System.Drawing.ColorTranslator.FromHtml(btn_ls3Col.Text);
                                btn_ls3Col.BackColor = ls3col;
                                btn_ls3Col.ForeColor = ls3col;
                                System.Drawing.Color ls4col = System.Drawing.ColorTranslator.FromHtml(btn_ls4Col.Text);
                                btn_ls4Col.BackColor = ls4col;
                                btn_ls4Col.ForeColor = ls4col;
                                System.Drawing.Color ls5col = System.Drawing.ColorTranslator.FromHtml(btn_ls5Col.Text);
                                btn_ls5Col.BackColor = ls5col;
                                btn_ls5Col.ForeColor = ls5col;
                                System.Drawing.Color ls6col = System.Drawing.ColorTranslator.FromHtml(btn_ls6Col.Text);
                                btn_ls6Col.BackColor = ls6col;
                                btn_ls6Col.ForeColor = ls6col;
                                System.Drawing.Color ls7col = System.Drawing.ColorTranslator.FromHtml(btn_ls7Col.Text);
                                btn_ls7Col.BackColor = ls7col;
                                btn_ls7Col.ForeColor = ls7col;
                                System.Drawing.Color ls8col = System.Drawing.ColorTranslator.FromHtml(btn_ls8Col.Text);
                                btn_ls8Col.BackColor = ls8col;
                                btn_ls8Col.ForeColor = ls8col;
                                System.Drawing.Color raideffectsA = System.Drawing.ColorTranslator.FromHtml(btn_raidEffectsA.Text);
                                btn_raidEffectsA.BackColor = raideffectsA;
                                btn_raidEffectsA.ForeColor = raideffectsA;
                                System.Drawing.Color raideffectsB = System.Drawing.ColorTranslator.FromHtml(btn_raidEffectsB.Text);
                                btn_raidEffectsB.BackColor = raideffectsB;
                                btn_raidEffectsB.ForeColor = raideffectsB;
                                System.Drawing.Color timercol = System.Drawing.ColorTranslator.FromHtml(btn_timerCol.Text);
                                btn_timerCol.BackColor = timercol;
                                btn_timerCol.ForeColor = timercol;
                                System.Drawing.Color dpsLimitCol = System.Drawing.ColorTranslator.FromHtml(btn_DPSLimitCol.Text);
                                btn_DPSLimitCol.BackColor = dpsLimitCol;
                                btn_DPSLimitCol.ForeColor = dpsLimitCol;


                                updateState("static", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                                ChromaReady = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Error loading settings: " + ex.Message;
                }
                xReader.Close();
            }
            else
            {
                RestoreDefaults();
            }
        }

        //Save settings to config file
        void SaveSettings()
        {
            FileStream fs = new FileStream(settingsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            XmlTextWriter xWriter = new XmlTextWriter(fs, Encoding.UTF8);
            xWriter.Formatting = Formatting.Indented;
            xWriter.Indentation = 1;
            xWriter.IndentChar = '\t';
            xWriter.WriteStartDocument(true);
            xWriter.WriteStartElement("Config");    // <Config>
            xWriter.WriteStartElement("SettingsSerializer");    // <Config><SettingsSerializer>
            xmlSettings.ExportToXml(xWriter);   // Fill the SettingsSerializer XML
            xWriter.WriteEndElement();  // </SettingsSerializer>
            xWriter.WriteEndElement();  // </Config>
            xWriter.WriteEndDocument(); // Tie up loose ends (shouldn't be any)
            xWriter.Flush();    // Flush the file buffer to disk
            xWriter.Close();
        }

        static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }
            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        //Setup device SDKs and Init
        void InitializeDevices()
        {
            chk_deviceKeyboard.ForeColor = System.Drawing.Color.LightGray;
            chk_deviceMouse.ForeColor = System.Drawing.Color.LightGray;
            chk_deviceKeypad.ForeColor = System.Drawing.Color.LightGray;
            chk_deviceMousepad.ForeColor = System.Drawing.Color.LightGray;
            chk_deviceHeadset.ForeColor = System.Drawing.Color.LightGray;
            chk_deviceCorsair.ForeColor = System.Drawing.Color.LightGray;
            chk_deviceLogitech.ForeColor = System.Drawing.Color.LightGray;

            //INIT REFERENCE SDKS

            //LOGITECH
            if (Process.GetProcessesByName("LCore").Length > 0)
            {
                LogitechGSDK.LogiLedInit();
                LogitechSDK = true;
            }
            else
            {
                LogitechSDK = false;
            }

            if (LogitechSDK == true)
            {
                chk_deviceLogitech.Checked = true;
                chk_deviceLogitech.AutoCheck = true;
                chk_deviceLogitech.ForeColor = SystemColors.ControlText;
            }
            else
            {
                chk_deviceLogitech.Checked = false;
                chk_deviceLogitech.AutoCheck = false;
                chk_deviceLogitech.ForeColor = System.Drawing.Color.LightGray;
            }
            
            //CORSAIR
            if (File.Exists(ProgramFilesx86() + @"\Corsair\Corsair Utility Engine\CorsairHID.exe"))
            {
                if (StartUp == false)
                {
                    if (!File.Exists(AppDataPath + @"\Advanced Combat Tracker\Plugins\CUESDK_2013.dll"))
                    {
                        DialogResult result = MessageBox.Show("The Corsair SDK has not been initialized on your system. Would you like to do it now?", "CUE SDK Not Found", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes)
                        {
                            try
                            {
                                File.Copy(pluginEnviroment + @"\lib\CUESDK_2013.dll", AppDataPath + @"\Advanced Combat Tracker\Plugins\CUESDK_2013.dll", true);
                                MessageBox.Show("Corsair SDK has been enabled. Please restart ACT and run CUE to enable Corsair support.");
                                CorsairSDK = false;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Chromatics was unable to initialize the Corsair SDK. Please try again later. (Error Code: " + ex.Message + ")");
                                CorsairSDK = false;
                            }

                        }
                        else
                        {
                            MessageBox.Show("Corsair devices will be temporarily disabled. Please restart ACT if you wish to initialize the SDK.");
                            CorsairSDK = false;
                        }
                    }
                    else
                    {
                        try
                        {
                            CueSDK.Initialize();
                            CorsairSDK = true;
                            MessageBox.Show("Initialized with " + CueSDK.LoadedArchitecture + " -SDK","DEBUG");
                        }
                        catch (CUEException ex)
                        {
                            if (ex.Error.ToString() == "ServerNotFound")
                            {
                                CorsairSDK = false;
                                //CorsairState = 2;
                                Debug.WriteLine(ex.Error.ToString());
                                MessageBox.Show("CUE (Corsair Utility Engine) is currently not running or not installed. Please install or run the program and restart ACT to enable Corsair Support. If you are still getting this error message, please select 'Enable SDK' in the CUE application.");
                            }
                            else
                            {
                                CorsairSDK = false;
                                MessageBox.Show("An unknown error occurred while trying to load the Corsair libraries. Corsair support will be disabled.");
                            }
                        }
                        catch (WrapperException ex)
                        {
                            if (ex.Message == "CueSDK is already initialized.")
                            {
                                CorsairSDK = false;
                                //CorsairState = 2;
                                MessageBox.Show("An error occured where ACT attempted to start the Corsair Engine twice. Please restart ACT to re-enable Corsair device support.");
                            }
                            else
                            {
                                CorsairSDK = false;
                                MessageBox.Show("An unknown error occurred while trying to load the Corsair libraries. Corsair support will be disabled.");
                            }
                        }
                    }
                }
            }

            if (CorsairSDK == true)
            {
                corKeyboard = CueSDK.KeyboardSDK;
                CorsairMouse corMouse = CueSDK.MouseSDK;
                CorsairHeadset corHeadset = CueSDK.HeadsetSDK;


                if (corKeyboard != null)
                {
                    CorsairKeyboardDetect = true;
                    chk_deviceCorsair.Checked = true;
                    chk_deviceCorsair.AutoCheck = true;
                    chk_deviceCorsair.ForeColor = SystemColors.ControlText;
                    
                }
                if (corMouse != null)
                {
                    CorsairMouseDetect = true;
                    chk_deviceCorsair.Checked = true;
                    chk_deviceCorsair.AutoCheck = true;
                    chk_deviceCorsair.ForeColor = SystemColors.ControlText;
                }
                if (corHeadset != null)
                {
                    CorsairHeadsetDetect = true;
                    chk_deviceCorsair.Checked = true;
                    chk_deviceCorsair.AutoCheck = true;
                    chk_deviceCorsair.ForeColor = SystemColors.ControlText;
                }
                
            }
            else
            {
                chk_deviceCorsair.Checked = false;
                chk_deviceCorsair.AutoCheck = false;
                chk_deviceCorsair.ForeColor = System.Drawing.Color.LightGray;
            }

            //RAZER
            if (Chroma.IsSdkAvailable() == false)
            {
                RazerSDK = false;
            }

            if (RazerSDK != false)
            {
                if (Chroma.Instance.Initialized != true)
                {
                    Chroma.Instance.Uninitialize();
                }
                else
                {
                    Chroma.Instance.Initialize();
                }
                Chroma chroma = (Chroma)Chroma.Instance;


                //Razer device auto-detection disabled due to bug in Razer SDK

                //Keyboards
                //if (chroma.Query(new Guid(0x2ea1bb63, 0xca28, 0x428d, 0x9f, 0x06, 0x19, 0x6b, 0x88, 0x33, 0x0b, 0xbb)).Connected == true) { chk_deviceKeyboard.Checked = true; chk_deviceKeyboard.AutoCheck = true; chk_deviceKeyboard.ForeColor = SystemColors.ControlText; } //Blackwidow
                //if (chroma.Query(new Guid(0xed1c1b82, 0xbfbe, 0x418f, 0xb4, 0x9d, 0xd0, 0x3f, 0x5, 0xb1, 0x49, 0xdf)).Connected == true) { chk_deviceKeyboard.Checked = true; chk_deviceKeyboard.AutoCheck = true; chk_deviceKeyboard.ForeColor = SystemColors.ControlText; } //BlackwidowTE
                //if (chroma.Query(new Guid(0x18c5ad9b, 0x4326, 0x4828, 0x92, 0xc4, 0x26, 0x69, 0xa6, 0x6d, 0x22, 0x83)).Connected == true) { chk_deviceKeyboard.Checked = true; chk_deviceKeyboard.AutoCheck = true; chk_deviceKeyboard.ForeColor = SystemColors.ControlText; } //Deathstalker

                //Mouse
                //if (chroma.Query(new Guid(0xaec50d91, 0xb1f1, 0x452f, 0x8e, 0x16, 0x7b, 0x73, 0xf3, 0x76, 0xfd, 0xf3)).Connected == true) { chk_deviceMouse.Checked = true; chk_deviceMouse.AutoCheck = true; chk_deviceMouse.ForeColor = SystemColors.ControlText; } //DeathAdder
                //if (chroma.Query(new Guid(0xff8a5929, 0x4512, 0x4257, 0x8d, 0x59, 0xc6, 0x47, 0xbf, 0x99, 0x35, 0xd0)).Connected == true) { chk_deviceMouse.Checked = true; chk_deviceMouse.AutoCheck = true; chk_deviceMouse.ForeColor = SystemColors.ControlText; } //Diamondback
                //if (chroma.Query(new Guid(0x7ec00450, 0xe0ee, 0x4289, 0x89, 0xd5, 0xd, 0x87, 0x9c, 0x19, 0x6, 0x1a)).Connected == true) { chk_deviceMouse.Checked = true; chk_deviceMouse.AutoCheck = true; chk_deviceMouse.ForeColor = SystemColors.ControlText; } //MambaTE

                //Keypad
                //if (chroma.Query(new Guid(0xf0545c, 0xe180, 0x4ad1, 0x8e, 0x8a, 0x41, 0x90, 0x61, 0xce, 0x50, 0x5e)).Connected == true) { chk_deviceKeypad.Checked = true; chk_deviceKeypad.AutoCheck = true; chk_deviceKeypad.ForeColor = SystemColors.ControlText; } //Tatarus
                //if (chroma.Query(new Guid(0x9d24b0ab, 0x162, 0x466c, 0x96, 0x40, 0x7a, 0x92, 0x4a, 0xa4, 0xd9, 0xfd)).Connected == true) { chk_deviceKeypad.Checked = true; chk_deviceKeypad.AutoCheck = true; chk_deviceKeypad.ForeColor = SystemColors.ControlText; } //Orbweaver

                //Mousepad
                //if (chroma.Query(new Guid(0x80f95a94, 0x73d2, 0x48ca, 0xae, 0x9a, 0x9, 0x86, 0x78, 0x9a, 0x9a, 0xf2)).Connected == true) { chk_deviceMousepad.Checked = true; chk_deviceMousepad.AutoCheck = true; chk_deviceMousepad.ForeColor = SystemColors.ControlText; } //Firefly

                //Headset
                //if (chroma.Query(new Guid(0xcd1e09a5, 0xd5e6, 0x4a6c, 0xa9, 0x3b, 0xe6, 0xd9, 0xbf, 0x1d, 0x20, 0x92)).Connected == true) { chk_deviceHeadset.Checked = true; chk_deviceHeadset.AutoCheck = true; chk_deviceHeadset.ForeColor = SystemColors.ControlText; } //Kraken 7.1


                chk_deviceKeyboard.Checked = true; chk_deviceKeyboard.AutoCheck = true; chk_deviceKeyboard.ForeColor = SystemColors.ControlText;
                chk_deviceMouse.Checked = true; chk_deviceMouse.AutoCheck = true; chk_deviceMouse.ForeColor = SystemColors.ControlText;
                chk_deviceKeypad.Checked = true; chk_deviceKeypad.AutoCheck = true; chk_deviceKeypad.ForeColor = SystemColors.ControlText;
                chk_deviceMousepad.Checked = true; chk_deviceMousepad.AutoCheck = true; chk_deviceMousepad.ForeColor = SystemColors.ControlText;
                chk_deviceHeadset.Checked = true; chk_deviceHeadset.AutoCheck = true; chk_deviceHeadset.ForeColor = SystemColors.ControlText;
            }

            ResetDevices();
            if (rescan == true)
            {
                if (RazerSDK == false)
                {
                    MessageBox.Show("The Razer SDK (RzChromaSDK64.dll) Could not be found on this computer. Please install Synapse and restart ACT if you wish to use Chromatics with Razer devices.");
                }
                if (CorsairSDK == false)
                {
                    MessageBox.Show("CUE (Corsair Utility Engine) is currently not running or not installed. Please install or run the program and restart ACT to enable Corsair Support. If you are still getting this error message, please select 'Enable SDK' in the CUE application.");
                }
                if (LogitechSDK == false)
                {
                    MessageBox.Show("Logitech Gaming Software is currently not running or not installed.Please install or run the program and restart ACT to enable Logitech Support.");
                }
                rescan = false;
            }

            StartUp = true;
        }

        //Reset devices if requested
        void ResetDevices()
        {
            if (chk_deviceKeyboard.Checked && RazerSDK != false) {
                DeviceKeyboard = true;
            } else {
                DeviceKeyboard = false;
            }
            if (chk_deviceKeypad.Checked && RazerSDK != false)
            {
                DeviceKeypad = true;
            }
            else
            {
                DeviceKeypad = false;
            }
            if (chk_deviceMouse.Checked && RazerSDK != false)
            {
                DeviceMouse = true;
            }
            else
            {
                DeviceMouse = false;
            }
            if (chk_deviceMousepad.Checked && RazerSDK != false)
            {
                DeviceMousepad = true;
            }
            else
            {
                DeviceMousepad = false;
            }
            if (chk_deviceHeadset.Checked && RazerSDK != false)
            {
                DeviceHeadset = true;
            }
            else
            {
                DeviceHeadset = false;
            }
            if (chk_deviceLogitech.Checked && LogitechSDK != false)
            {
                DeviceLogitech = true;
            }
            else
            {
                DeviceLogitech = false;
            }
            if (chk_deviceCorsair.Checked && CorsairSDK != false)
            {
                CorsairKeyboardDetect = true;
                CorsairMouseDetect = true;
                CorsairHeadsetDetect = true;
            }
            else
            {
                CorsairKeyboardDetect = false;
                CorsairMouseDetect = false;
                CorsairHeadsetDetect = false;
            }
        }

        //Restore default settings
        void RestoreDefaults()
        {
            chk_enableEmnity.Checked = true;
            chk_enableTimers.Checked = false;
            chk_enableTriggers.Checked = false;
            chk_GoldSaucerVegas.Checked = false;
            chk_raidEffects.Checked = false;
            chk_reactiveWeather.Checked = false;
            chk_say.Checked = false;
            chk_fc.Checked = false;
            chk_tell.Checked = true;
            chk_yell.Checked = false;
            chk_shout.Checked = false;
            chk_party.Checked = false;
            chk_alliance.Checked = false;
            chk_ls1.Checked = false;
            chk_ls2.Checked = false;
            chk_ls3.Checked = false;
            chk_ls4.Checked = false;
            chk_ls5.Checked = false;
            chk_ls6.Checked = false;
            chk_ls7.Checked = false;
            chk_ls8.Checked = false;
            chk_DPSLimit.Checked = false;
            cb_TimerCount.SelectedItem = "3";
            cb_TriggerCount.SelectedItem = "4";
            cb_Event.SelectedItem = "Warning";
            cb_TriggerSpeed.SelectedItem = "Fast";
            System.Drawing.Color _defaultCol = System.Drawing.Color.DeepSkyBlue;
            System.Drawing.Color _emnityCol = System.Drawing.Color.Red;
            System.Drawing.Color _triggerCol = System.Drawing.Color.BlueViolet;
            btn_defaultCol.BackColor = _defaultCol;
            btn_defaultCol.ForeColor = _defaultCol;
            btn_emnityCol.BackColor = _emnityCol;
            btn_emnityCol.ForeColor = _emnityCol;
            btn_triggerCol.BackColor = _triggerCol;
            btn_triggerCol.ForeColor = _triggerCol;
            btn_defaultCol.Text = ColorTranslator.ToHtml(_defaultCol);
            btn_emnityCol.Text = ColorTranslator.ToHtml(_emnityCol);
            btn_triggerCol.Text = ColorTranslator.ToHtml(_triggerCol);
            System.Drawing.Color saycol = System.Drawing.Color.White;
            btn_sayCol.BackColor = saycol;
            btn_sayCol.ForeColor = saycol;
            btn_sayCol.Text = ColorTranslator.ToHtml(saycol);
            System.Drawing.Color tellcol = System.Drawing.Color.White;
            btn_tellCol.BackColor = tellcol;
            btn_tellCol.ForeColor = tellcol;
            btn_tellCol.Text = ColorTranslator.ToHtml(tellcol);
            System.Drawing.Color yellcol = System.Drawing.Color.White;
            btn_yellCol.BackColor = yellcol;
            btn_yellCol.ForeColor = yellcol;
            btn_yellCol.Text = ColorTranslator.ToHtml(yellcol);
            System.Drawing.Color shoutcol = System.Drawing.Color.White;
            btn_shoutCol.BackColor = shoutcol;
            btn_shoutCol.ForeColor = shoutcol;
            btn_shoutCol.Text = ColorTranslator.ToHtml(shoutcol);
            System.Drawing.Color partycol = System.Drawing.Color.White;
            btn_partyCol.BackColor = partycol;
            btn_partyCol.ForeColor = partycol;
            btn_partyCol.Text = ColorTranslator.ToHtml(partycol);
            System.Drawing.Color alliancecol = System.Drawing.Color.White;
            btn_allianceCol.BackColor = alliancecol;
            btn_allianceCol.ForeColor = alliancecol;
            btn_allianceCol.Text = ColorTranslator.ToHtml(alliancecol);
            System.Drawing.Color fccol = System.Drawing.Color.White;
            btn_fcCol.BackColor = fccol;
            btn_fcCol.ForeColor = fccol;
            btn_fcCol.Text = ColorTranslator.ToHtml(fccol);
            System.Drawing.Color ls1col = System.Drawing.Color.White;
            btn_ls1Col.BackColor = ls1col;
            btn_ls1Col.ForeColor = ls1col;
            btn_ls1Col.Text = ColorTranslator.ToHtml(ls1col);
            System.Drawing.Color ls2col = System.Drawing.Color.White;
            btn_ls2Col.BackColor = ls2col;
            btn_ls2Col.ForeColor = ls2col;
            btn_ls2Col.Text = ColorTranslator.ToHtml(ls2col);
            System.Drawing.Color ls3col = System.Drawing.Color.White;
            btn_ls3Col.BackColor = ls3col;
            btn_ls3Col.ForeColor = ls3col;
            btn_ls3Col.Text = ColorTranslator.ToHtml(ls3col);
            System.Drawing.Color ls4col = System.Drawing.Color.White;
            btn_ls4Col.BackColor = ls4col;
            btn_ls4Col.ForeColor = ls4col;
            btn_ls4Col.Text = ColorTranslator.ToHtml(ls4col);
            System.Drawing.Color ls5col = System.Drawing.Color.White;
            btn_ls5Col.BackColor = ls5col;
            btn_ls5Col.ForeColor = ls5col;
            btn_ls5Col.Text = ColorTranslator.ToHtml(ls5col);
            System.Drawing.Color ls6col = System.Drawing.Color.White;
            btn_ls6Col.BackColor = ls6col;
            btn_ls6Col.ForeColor = ls6col;
            btn_ls6Col.Text = ColorTranslator.ToHtml(ls6col);
            System.Drawing.Color ls7col = System.Drawing.Color.White;
            btn_ls7Col.BackColor = ls7col;
            btn_ls7Col.ForeColor = ls7col;
            btn_ls7Col.Text = ColorTranslator.ToHtml(ls7col);
            System.Drawing.Color ls8col = System.Drawing.Color.White;
            btn_ls8Col.BackColor = ls8col;
            btn_ls8Col.ForeColor = ls8col;
            btn_ls8Col.Text = ColorTranslator.ToHtml(ls8col);
            System.Drawing.Color raideffectsA = System.Drawing.Color.Gold;
            btn_raidEffectsA.BackColor = raideffectsA;
            btn_raidEffectsA.ForeColor = raideffectsA;
            btn_raidEffectsA.Text = ColorTranslator.ToHtml(raideffectsA);
            System.Drawing.Color raideffectsB = System.Drawing.Color.DeepSkyBlue;
            btn_raidEffectsB.BackColor = raideffectsB;
            btn_raidEffectsB.ForeColor = raideffectsB;
            btn_raidEffectsB.Text = ColorTranslator.ToHtml(raideffectsB);
            btn_timerCol.Text = ColorTranslator.ToHtml(dia_col4.Color);
            System.Drawing.Color timercol = System.Drawing.Color.LimeGreen;
            btn_timerCol.BackColor = timercol;
            btn_timerCol.ForeColor = timercol;
            btn_timerCol.Text = ColorTranslator.ToHtml(timercol);
            txt_DPSlimit.Value = 1000;
            txt_DPSNotify.Value = -1;
            System.Drawing.Color _DPSLimitCol = System.Drawing.Color.White;
            btn_DPSLimitCol.BackColor = _DPSLimitCol;
            btn_DPSLimitCol.ForeColor = _DPSLimitCol;
            btn_DPSLimitCol.Text = ColorTranslator.ToHtml(_DPSLimitCol);

            SaveSettings();
            LoadSettings();
        }

        //Handle design element events
        private void pic_Logo_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/roxaskeyheart/Chromatics");
        }
        
        private void chk_enableEmnity_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_enableTriggers_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_enableTimers_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }
        
        private void chk_deviceKeyboard_CheckedChanged(object sender, EventArgs e)
        {
            if (StartUp == true)
            {
                if (RazerSDK != false)
                {
                    ResetDevices();
                }
                else
                {
                    MessageBox.Show("The Razer SDK (RzChromaSDK64.dll) Could not be found on this computer. Please install Synapse and restart ACT if you wish to use Chromatics with Razer devices.");
                }
            }
        }

        private void chk_deviceKeypad_CheckedChanged(object sender, EventArgs e)
        {
            if (StartUp == true)
            {
                if (RazerSDK != false)
                {
                    ResetDevices();
                }
                else
                {
                    MessageBox.Show("The Razer SDK (RzChromaSDK64.dll) Could not be found on this computer. Please install Synapse and restart ACT if you wish to use Chromatics with Razer devices.");
                }
            }
        }

        private void chk_deviceMouse_CheckedChanged(object sender, EventArgs e)
        {
            if (StartUp == true)
            {
                if (RazerSDK != false)
                {
                    ResetDevices();
                }
                else
                {
                    MessageBox.Show("The Razer SDK (RzChromaSDK64.dll) Could not be found on this computer. Please install Synapse and restart ACT if you wish to use Chromatics with Razer devices.");
                }
            }
        }

        private void chk_deviceMousepad_CheckedChanged(object sender, EventArgs e)
        {
            if (StartUp == true)
            {
                if (RazerSDK != false)
                {
                    ResetDevices();
                }
                else
                {
                    MessageBox.Show("The Razer SDK (RzChromaSDK64.dll) Could not be found on this computer. Please install Synapse and restart ACT if you wish to use Chromatics with Razer devices.");
                }
            }
        }

        private void chk_deviceHeadset_CheckedChanged(object sender, EventArgs e)
        {
            if (StartUp == true)
            {
                if (RazerSDK != false)
                {
                    ResetDevices();
                }
                else
                {
                    MessageBox.Show("The Razer SDK (RzChromaSDK64.dll) Could not be found on this computer. Please install Synapse and restart ACT if you wish to use Chromatics with Razer devices.");
                }
            }
        }

        private void chk_deviceLogitech_CheckedChanged(object sender, EventArgs e)
        {
            if (StartUp == true)
            {
                if (LogitechSDK != false)
                {
                    ResetDevices();
                    SaveSettings();
                }
                else
                {
                    MessageBox.Show("Logitech Gaming Software is currently not running or not installed. Please install or run the program and restart ACT to enable Logitech Support.");
                }
            }
        }

        private void chk_deviceCorsair_CheckedChanged(object sender, EventArgs e)
        {
            if (StartUp == true)
            {
                if (CorsairSDK != false)
                {
                    ResetDevices();
                }
                else
                {
                    MessageBox.Show("CUE (Corsair Utility Engine) is currently not running or not installed. Please install or run the program and restart ACT to enable Corsair Support. If you are still getting this error message, please select 'Enable SDK' in the CUE application.");
                }
            }
        }

        private void cb_TriggerCount_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb_TriggerCount.SelectedItem.ToString() == "1")
            {
                maxtick = 2;
            }
            else if (cb_TriggerCount.SelectedItem.ToString() == "2")
            {
                maxtick = 4;
            }
            else if (cb_TriggerCount.SelectedItem.ToString() == "3")
            {
                maxtick = 6;
            }
            else if (cb_TriggerCount.SelectedItem.ToString() == "4")
            {
                maxtick = 8;
            }
            else if (cb_TriggerCount.SelectedItem.ToString() == "5")
            {
                maxtick = 10;
            }
            SaveSettings();
        }

        private void cb_TimerCount_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb_TimerCount.SelectedItem.ToString() == "1")
            {
                timercount = 2;
            }
            else if (cb_TimerCount.SelectedItem.ToString() == "2")
            {
                timercount = 4;
            }
            else if (cb_TimerCount.SelectedItem.ToString() == "3")
            {
                timercount = 6;
            }
            else if (cb_TimerCount.SelectedItem.ToString() == "4")
            {
                timercount = 8;
            }
            else if (cb_TimerCount.SelectedItem.ToString() == "5")
            {
                timercount = 10;
            }
            SaveSettings();
        }

        private void cb_Event_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb_Event.SelectedItem.ToString() == "Expire")
            {
                timerevent = "Expire";
            }
            else if (cb_Event.SelectedItem.ToString() == "Warning")
            {
                timerevent = "Warning";
            }
            else if (cb_Event.SelectedItem.ToString() == "Removed")
            {
                timerevent = "Removed";
            }
            SaveSettings();
        }

        private void btn_timerCol_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_col4.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_timerCol.BackColor = dia_col4.Color;
                btn_timerCol.ForeColor = dia_col4.Color;
                btn_timerCol.Text = ColorTranslator.ToHtml(dia_col4.Color);
                SaveSettings();
            }
        }

        private void btn_DPSLimitCol_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_DPSlimit.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_DPSLimitCol.BackColor = dia_DPSlimit.Color;
                btn_DPSLimitCol.ForeColor = dia_DPSlimit.Color;
                btn_DPSLimitCol.Text = ColorTranslator.ToHtml(dia_DPSlimit.Color);
                SaveSettings();
            }
        }

        private void txt_DPSlimit_ValueChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void txt_DPSNotify_ValueChanged(object sender, EventArgs e)
        {
            if (txt_DPSNotify.Value > txt_DPSlimit.Value)
            {
                MessageBox.Show("Notify DPS value cannot be higher than Target DPS value.");
                txt_DPSNotify.Value = txt_DPSlimit.Value;
                txt_DPSNotify.ForeColor = System.Drawing.SystemColors.WindowText;
                txt_DPSNotify.BackColor = System.Drawing.SystemColors.Window;
            }
            else if (txt_DPSNotify.Value == -1)
            {
                txt_DPSNotify.ForeColor = System.Drawing.Color.DarkGray;
                txt_DPSNotify.BackColor = System.Drawing.Color.LightGray;
            }
            else
            {
                txt_DPSNotify.ForeColor = System.Drawing.SystemColors.WindowText;
                txt_DPSNotify.BackColor = System.Drawing.SystemColors.Window;
            }

            SaveSettings();
        }

        private void cb_TriggerSpeed_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb_TriggerSpeed.SelectedItem.ToString() == "Very Slow")
            {
                tickspeed = 700;
            }
            else if (cb_TriggerSpeed.SelectedItem.ToString() == "Slow")
            {
                tickspeed = 500;
            }
            else if (cb_TriggerSpeed.SelectedItem.ToString() == "Moderate")
            {
                tickspeed = 300;
            }
            else if (cb_TriggerSpeed.SelectedItem.ToString() == "Fast")
            {
                tickspeed = 200;
            }
            else if (cb_TriggerSpeed.SelectedItem.ToString() == "Very Fast")
            {
                tickspeed = 100;
            }
            SaveSettings();
        }

        private void btn_defaultCol_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_col1.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_defaultCol.BackColor = dia_col1.Color;
                btn_defaultCol.ForeColor = dia_col1.Color;
                btn_defaultCol.Text = ColorTranslator.ToHtml(dia_col1.Color);

                if (state == 1)
                {
                    updateState("static", dia_col1.Color, dia_col1.Color);
                }

                SaveSettings();
            }
        }

        private void btn_emnityCol_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_col2.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_emnityCol.BackColor = dia_col2.Color;
                btn_emnityCol.ForeColor = dia_col2.Color;
                btn_emnityCol.Text = ColorTranslator.ToHtml(dia_col2.Color);
                SaveSettings();
            }
        }

        private void btn_triggerCol_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_col3.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_triggerCol.BackColor = dia_col3.Color;
                btn_triggerCol.ForeColor = dia_col3.Color;
                btn_triggerCol.Text = ColorTranslator.ToHtml(dia_col3.Color);
                SaveSettings();
            }
        }

        private void btn_rescan_Click(object sender, EventArgs e)
        {
            rescan = true;

            InitializeDevices();
        }

        private void chk_say_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_shout_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_tell_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_yell_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_fc_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_party_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_alliance_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_ls1_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_ls2_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_ls3_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_ls4_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_ls5_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_ls6_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_ls7_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_ls8_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void chk_GoldSaucerVegas_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_GoldSaucerVegas.Checked && currentZone == "The Golden Saucer")
            {
                updateState("wave", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                state = 3;
            }
            else
            {
                updateState("static", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                state = 1;
            }
            SaveSettings();
        }

        private void chk_raidEffects_CheckChanged(object sender, EventArgs e)
        {
            if (chk_raidEffects.Checked && raidZoneList.Contains(currentZone))
            {
                updateState("breath", btn_raidEffectsA.BackColor, btn_raidEffectsB.BackColor);
                state = 4;
            }
            else
            {
                state = 1;
            }
            SaveSettings();
        }

        private void chk_reactiveWeather_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_reactiveWeather.Checked)
            {
                skyWatcher.Enabled = true;
                setWeather(calculateWeather(currentZone));
                state = 6;
            }
            else
            {
                skyWatcher.Enabled = false;
                updateState("static", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                state = 1;
            }
            SaveSettings();
        }

        private void chk_DPSLimit_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_DPSLimit.Checked == false)
            {
                if (inCombat == true && chk_enableEmnity.Checked)
                {
                    dpsloop.Stop();
                    dpstickstart = false;
                    dpsloop.Enabled = false;
                    updateState("static", btn_emnityCol.BackColor, btn_defaultCol.BackColor);
                }
            }

            SaveSettings();
        }

        private void btn_raidEffectsA_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_raidA.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_raidEffectsA.BackColor = dia_raidA.Color;
                btn_raidEffectsA.ForeColor = dia_raidA.Color;
                btn_raidEffectsA.Text = ColorTranslator.ToHtml(dia_raidA.Color);
                SaveSettings();

                if (state == 4)
                {
                    LogitechGSDK.LogiLedStopEffects();
                    Thread.Sleep(100);
                    updateState("breath", dia_raidA.Color, btn_raidEffectsB.BackColor);
                }
            }
        }

        private void btn_raidEffectsB_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_raidB.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_raidEffectsB.BackColor = dia_raidB.Color;
                btn_raidEffectsB.ForeColor = dia_raidB.Color;
                btn_raidEffectsB.Text = ColorTranslator.ToHtml(dia_raidB.Color);
                SaveSettings();

                if (state == 4)
                {
                    LogitechGSDK.LogiLedStopEffects();
                    Thread.Sleep(100);
                    updateState("breath", btn_raidEffectsA.BackColor, dia_raidB.Color);
                }
            }
        }

        private void btn_sayCol_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_say.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_sayCol.BackColor = dia_say.Color;
                btn_sayCol.ForeColor = dia_say.Color;
                btn_sayCol.Text = ColorTranslator.ToHtml(dia_say.Color);
                SaveSettings();
            }
        }

        private void btn_tellCol_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_tell.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_tellCol.BackColor = dia_tell.Color;
                btn_tellCol.ForeColor = dia_tell.Color;
                btn_tellCol.Text = ColorTranslator.ToHtml(dia_tell.Color);
                SaveSettings();
            }
        }

        private void btn_yellCol_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_yell.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_yellCol.BackColor = dia_yell.Color;
                btn_yellCol.ForeColor = dia_yell.Color;
                btn_yellCol.Text = ColorTranslator.ToHtml(dia_yell.Color);
                SaveSettings();
            }
        }

        private void btn_shoutCol_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_shout.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_shoutCol.BackColor = dia_shout.Color;
                btn_shoutCol.ForeColor = dia_shout.Color;
                btn_shoutCol.Text = ColorTranslator.ToHtml(dia_shout.Color);
                SaveSettings();
            }
        }

        private void btn_allianceCol_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_alliance.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_allianceCol.BackColor = dia_alliance.Color;
                btn_allianceCol.ForeColor = dia_alliance.Color;
                btn_allianceCol.Text = ColorTranslator.ToHtml(dia_alliance.Color);
                SaveSettings();
            }
        }

        private void btn_partyCol_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_party.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_partyCol.BackColor = dia_party.Color;
                btn_partyCol.ForeColor = dia_party.Color;
                btn_partyCol.Text = ColorTranslator.ToHtml(dia_party.Color);
                SaveSettings();
            }
        }

        private void btn_ls1Col_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_ls1.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_ls1Col.BackColor = dia_ls1.Color;
                btn_ls1Col.ForeColor = dia_ls1.Color;
                btn_ls1Col.Text = ColorTranslator.ToHtml(dia_ls1.Color);
                SaveSettings();
            }
        }

        private void btn_ls2Col_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_ls2.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_ls2Col.BackColor = dia_ls2.Color;
                btn_ls2Col.ForeColor = dia_ls2.Color;
                btn_ls2Col.Text = ColorTranslator.ToHtml(dia_ls2.Color);
                SaveSettings();
            }
        }

        private void btn_ls3Col_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_ls3.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_ls3Col.BackColor = dia_ls3.Color;
                btn_ls3Col.ForeColor = dia_ls3.Color;
                btn_ls3Col.Text = ColorTranslator.ToHtml(dia_ls3.Color);
                SaveSettings();
            }
        }

        private void btn_ls4Col_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_ls4.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_ls4Col.BackColor = dia_ls4.Color;
                btn_ls4Col.ForeColor = dia_ls4.Color;
                btn_ls4Col.Text = ColorTranslator.ToHtml(dia_ls4.Color);
                SaveSettings();
            }
        }

        private void btn_ls5Col_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_ls5.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_ls5Col.BackColor = dia_ls5.Color;
                btn_ls5Col.ForeColor = dia_ls5.Color;
                btn_ls5Col.Text = ColorTranslator.ToHtml(dia_ls5.Color);
                SaveSettings();
            }
        }

        private void btn_ls6Col_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_ls6.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_ls6Col.BackColor = dia_ls6.Color;
                btn_ls6Col.ForeColor = dia_ls6.Color;
                btn_ls6Col.Text = ColorTranslator.ToHtml(dia_ls6.Color);
                SaveSettings();
            }
        }

        private void btn_ls7Col_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_ls7.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_ls7Col.BackColor = dia_ls7.Color;
                btn_ls7Col.ForeColor = dia_ls7.Color;
                btn_ls7Col.Text = ColorTranslator.ToHtml(dia_ls7.Color);
                SaveSettings();
            }
        }

        private void btn_ls8Col_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_ls8.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_ls8Col.BackColor = dia_ls8.Color;
                btn_ls8Col.ForeColor = dia_ls8.Color;
                btn_ls8Col.Text = ColorTranslator.ToHtml(dia_ls8.Color);
                SaveSettings();
            }
        }

        private void btn_fcCol_Click(object sender, EventArgs e)
        {
            DialogResult result = dia_fc.ShowDialog();
            if (result == DialogResult.OK)
            {
                btn_fcCol.BackColor = dia_fc.Color;
                btn_fcCol.ForeColor = dia_fc.Color;
                btn_fcCol.Text = ColorTranslator.ToHtml(dia_fc.Color);
                SaveSettings();
            }
        }


        //Handle device send/recieve
        private CancellationTokenSource CTS = new CancellationTokenSource();
        public void updateState(string type, System.Drawing.Color col, [Optional]System.Drawing.Color col2, [Optional]bool direction, [Optional]int speed)
        {
            if (type == "reset")
            {
                if (RazerSDK != false)
                {
                    if (DeviceHeadset == true) { Headset.Instance.Clear(); }
                    if (DeviceKeyboard == true) { Keyboard.Instance.Clear(); }
                    if (DeviceKeypad == true) { Keypad.Instance.Clear(); }
                    if (DeviceMouse == true) { Mouse.Instance.Clear(); }
                    if (DeviceMousepad == true) { Mousepad.Instance.Clear(); }
                }
                if (CorsairSDK != false)
                {
                    if (CorsairKeyboardDetect == true)
                    {
                        CorsairKeyboard corKeyboard = CueSDK.KeyboardSDK;
                        ListKeyGroup allGroup = new ListKeyGroup(corKeyboard)
                        { Brush = new SolidColorBrush(System.Drawing.Color.Black) };
                        corKeyboard.Update();

                    }
                    if (CorsairMouseDetect == true)
                    {
                        CorsairMouse corMouse = CueSDK.MouseSDK;
                        //Unsupported
                    }
                    if (CorsairHeadsetDetect == true)
                    {
                        CorsairHeadset corHeadset = CueSDK.HeadsetSDK;
                        //Unsupported
                    }
                }
                if (LogitechSDK != false)
                {
                    if (DeviceLogitech == true)
                    {
                        if (LogiEffectRunning == true)
                        {
                            LogitechGSDK.LogiLedStopEffects();
                            Thread.Sleep(100);
                        }
                        LogitechGSDK.LogiLedSetLighting(0, 0, 0);
                    }
                }
            }
            else if (type == "static")
            {
                if (RazerSDK != false)
                {
                    new Task(() =>
                    {
                        if (DeviceHeadset == true) { Headset.Instance.SetAll(Corale.Colore.Core.Color.FromSystemColor(col)); }
                        if (DeviceKeyboard == true)
                        {
                            Keyboard.Instance.SetAll(Corale.Colore.Core.Color.FromSystemColor(col));
                        }
                        if (DeviceKeypad == true) { Keypad.Instance.SetAll(Corale.Colore.Core.Color.FromSystemColor(col)); }
                        if (DeviceMouse == true) { Mouse.Instance.SetAll(Corale.Colore.Core.Color.FromSystemColor(col)); }
                        if (DeviceMousepad == true) { Mousepad.Instance.SetAll(Corale.Colore.Core.Color.FromSystemColor(col)); }
                    }).Start();

                }
                if (CorsairSDK != false)
                {
                    new Task(() =>
                    {
                    if (CorsairKeyboardDetect == true)
                    {
                        UpdateKeyboard(col);
                    }
                    if (CorsairMouseDetect == true)
                    {
                        CorsairMouse corMouse = CueSDK.MouseSDK;
                        //Unsupported
                    }
                    if (CorsairHeadsetDetect == true)
                    {
                        CorsairHeadset corHeadset = CueSDK.HeadsetSDK;
                        //Unsupported
                    }
                    }).Start();
                }
                if (LogitechSDK != false)
                {
                    new Task(() =>
                    {
                        if (DeviceLogitech == true)
                        {
                            if (LogiEffectRunning == true)
                            {
                                LogitechGSDK.LogiLedStopEffects();
                                Thread.Sleep(100);
                            }

                            LogitechGSDK.LogiLedSetLighting((int)Math.Ceiling((double)(col.R * 100) / 255), (int)Math.Ceiling((double)(col.G * 100) / 255), (int)Math.Ceiling((double)(col.B * 100) / 255));
                            LogiEffectRunning = false;
                        }
                    }).Start();
                }
            }
            else if (type == "transition")
            {
                if (RazerSDK != false)
                {
                    new Task(() =>
                    {
                        if (DeviceHeadset == true) { Headset.Instance.SetAll(Corale.Colore.Core.Color.FromSystemColor(col)); }
                        if (DeviceKeyboard == true)
                        {
                            transition(Corale.Colore.Core.Color.FromSystemColor(col), direction);
                        }
                        if (DeviceKeypad == true) { Keypad.Instance.SetAll(Corale.Colore.Core.Color.FromSystemColor(col)); }
                        if (DeviceMouse == true) { Mouse.Instance.SetAll(Corale.Colore.Core.Color.FromSystemColor(col)); }
                        if (DeviceMousepad == true) { Mousepad.Instance.SetAll(Corale.Colore.Core.Color.FromSystemColor(col)); }
                    }).Start();
                }
                if (CorsairSDK != false)
                {
                    new Task(() =>
                    {
                        if (CorsairKeyboardDetect == true)
                        {
                            UpdateKeyboard(col);
                        }
                        if (CorsairMouseDetect == true)
                        {
                            CorsairMouse corMouse = CueSDK.MouseSDK;
                            //Unsupported
                        }
                        if (CorsairHeadsetDetect == true)
                        {
                            CorsairHeadset corHeadset = CueSDK.HeadsetSDK;
                            //Unsupported
                        }
                    }).Start();
                }
                if (LogitechSDK != false)
                {
                    new Task(() =>
                    {
                        if (DeviceLogitech == true)
                        {
                            if (LogiEffectRunning == true)
                            {
                                LogitechGSDK.LogiLedStopEffects();
                                Thread.Sleep(100);
                            }
                            LogitechGSDK.LogiLedSetLighting((int)Math.Ceiling((double)(col.R * 100) / 255), (int)Math.Ceiling((double)(col.G * 100) / 255), (int)Math.Ceiling((double)(col.B * 100) / 255));
                            LogiEffectRunning = false;
                        }
                    }).Start();
                }
            }
            else if (type == "wave")
            {
                if (RazerSDK != false)
                {
                    new Task(() =>
                    {
                        if (DeviceHeadset == true) { Headset.Instance.SetEffect(Corale.Colore.Razer.Headset.Effects.Effect.SpectrumCycling); }
                        if (DeviceKeyboard == true) { Keyboard.Instance.SetWave(Corale.Colore.Razer.Keyboard.Effects.Direction.LeftToRight); }
                        if (DeviceKeypad == true) { Keypad.Instance.SetWave(Corale.Colore.Razer.Keypad.Effects.Direction.LeftToRight); }
                        if (DeviceMouse == true) { Mouse.Instance.SetWave(Corale.Colore.Razer.Mouse.Effects.Direction.FrontToBack); }
                        if (DeviceMousepad == true) { Mousepad.Instance.SetWave(Corale.Colore.Razer.Mousepad.Effects.Direction.LeftToRight); }
                    }).Start();
                }
                if (CorsairSDK != false)
                {
                    new Task(() =>
                    {
                        if (CorsairKeyboardDetect == true)
                        {
                            UpdateKeyboard(col);
                        }
                        if (CorsairMouseDetect == true)
                        {
                            CorsairMouse corMouse = CueSDK.MouseSDK;
                            //Unsupported
                        }
                        if (CorsairHeadsetDetect == true)
                        {
                            CorsairHeadset corHeadset = CueSDK.HeadsetSDK;
                            //Unsupported
                        }
                    }).Start();
                }
                if (LogitechSDK != false)
                {
                    new Task(() =>
                    {
                        if (DeviceLogitech == true)
                        {
                            if (LogiEffectRunning == true)
                            {
                                LogitechGSDK.LogiLedStopEffects();
                                Thread.Sleep(100);
                            }
                            
                            LogiColourCycle(col);
                            LogiEffectRunning = false;
                        }
                    }).Start();
                }
            }
            else if (type == "breath")
            {
                if (RazerSDK != false)
                {
                    new Task(() =>
                    {
                        if (DeviceHeadset == true) { Headset.Instance.SetBreathing(Corale.Colore.Core.Color.FromSystemColor(col)); }
                        if (DeviceKeyboard == true) { Keyboard.Instance.SetBreathing(Corale.Colore.Core.Color.FromSystemColor(col), Corale.Colore.Core.Color.FromSystemColor(col2)); }
                        if (DeviceKeypad == true) { Keypad.Instance.SetBreathing(Corale.Colore.Core.Color.FromSystemColor(col), Corale.Colore.Core.Color.FromSystemColor(col2)); }
                        if (DeviceMouse == true) { Mouse.Instance.SetBreathing(Corale.Colore.Core.Color.FromSystemColor(col), Corale.Colore.Core.Color.FromSystemColor(col2), Led.All); }
                        if (DeviceMousepad == true) { Mousepad.Instance.SetBreathing(Corale.Colore.Core.Color.FromSystemColor(col), Corale.Colore.Core.Color.FromSystemColor(col2)); }
                    }).Start();
                }
                if (CorsairSDK != false)
                {
                    new Task(() =>
                    {
                        if (CorsairKeyboardDetect == true)
                        {
                            UpdateKeyboard(col);
                        }
                        if (CorsairMouseDetect == true)
                        {
                            CorsairMouse corMouse = CueSDK.MouseSDK;
                            //Unsupported
                        }
                        if (CorsairHeadsetDetect == true)
                        {
                            CorsairHeadset corHeadset = CueSDK.HeadsetSDK;
                            //Unsupported
                        }
                    }).Start();
                }
                if (LogitechSDK != false)
                {
                    new Task(() =>
                    {
                        if (DeviceLogitech == true)
                        {
                            LogitechGSDK.LogiLedPulseLighting((int)Math.Ceiling((double)(col.R * 100) / 255), (int)Math.Ceiling((double)(col.G * 100) / 255), (int)Math.Ceiling((double)(col.B * 100) / 255), LogitechGSDK.LOGI_LED_DURATION_INFINITE, 60);
                            LogiEffectRunning = true;
                        }
                    }).Start();
                }

            }
            else if (type == "pulse")
            {
                if (RazerSDK != false)
                {
                    new Task(() =>
                    {
                        if (DeviceHeadset == true) { Headset.Instance.SetAll(Corale.Colore.Core.Color.FromSystemColor(col)); }
                        if (DeviceKeyboard == true)
                        {
                            transitionConst(Corale.Colore.Core.Color.FromSystemColor(col), Corale.Colore.Core.Color.FromSystemColor(col2), true, speed);
                            
                        }
                        if (DeviceKeypad == true) { Keypad.Instance.SetAll(Corale.Colore.Core.Color.FromSystemColor(col)); }
                        if (DeviceMouse == true) { Mouse.Instance.SetAll(Corale.Colore.Core.Color.FromSystemColor(col)); }
                        if (DeviceMousepad == true) { Mousepad.Instance.SetAll(Corale.Colore.Core.Color.FromSystemColor(col)); }
                    }, CTS.Token).Start();
                    RzPulse = true;
                }
                if (CorsairSDK != false)
                {
                    new Task(() =>
                    {
                        if (CorsairKeyboardDetect == true)
                        {
                            UpdateKeyboard(col);
                        }
                        if (CorsairMouseDetect == true)
                        {
                            CorsairMouse corMouse = CueSDK.MouseSDK;
                            //Unsupported
                        }
                        if (CorsairHeadsetDetect == true)
                        {
                            CorsairHeadset corHeadset = CueSDK.HeadsetSDK;
                            //Unsupported
                        }
                    }).Start();
                }
                if (LogitechSDK != false)
                {
                    new Task(() =>
                    {
                        if (DeviceLogitech == true)
                        {
                            if (LogiEffectRunning == true)
                            {
                                LogitechGSDK.LogiLedStopEffects();
                                Thread.Sleep(100);
                            }

                            LogitechGSDK.LogiLedSetLighting((int)Math.Ceiling((double)(col.R * 100) / 255), (int)Math.Ceiling((double)(col.G * 100) / 255), (int)Math.Ceiling((double)(col.B * 100) / 255));
                            LogiEffectRunning = false;
                        }
                    }).Start();
                }
            }
        }

        //Handle effects
        static readonly object _LogiColourCycle = new object();
        private void LogiColourCycle(System.Drawing.Color col)
        {
            lock(_LogiColourCycle)
            {
                while (state == 3)
                {
                    for (int x = 0; x <= 250; x += 5)
                    {
                        if (state != 3) { break; }
                        Thread.Sleep(10);
                        LogitechGSDK.LogiLedSetLighting((int)Math.Ceiling((double)(250 * 100) / 255), (int)Math.Ceiling((double)(x * 100) / 255), 0);
                    }
                    for (int x = 250; x >= 5; x -= 5)
                    {
                        if (state != 3) { break; }
                        Thread.Sleep(10);
                        LogitechGSDK.LogiLedSetLighting((int)Math.Ceiling((double)(x * 100) / 255), (int)Math.Ceiling((double)(250 * 100) / 255), 0);
                    }
                    for (int x = 0; x <= 250; x += 5)
                    {
                        if (state != 3) { break; }
                        Thread.Sleep(10);
                        LogitechGSDK.LogiLedSetLighting((int)Math.Ceiling((double)(x * 100) / 255), (int)Math.Ceiling((double)(250 * 100) / 255), 0);
                    }
                    for (int x = 250; x >= 5; x -= 5)
                    {
                        if (state != 3) { break; }
                        Thread.Sleep(10);
                        LogitechGSDK.LogiLedSetLighting(0, (int)Math.Ceiling((double)(x * 100) / 255), (int)Math.Ceiling((double)(250 * 100) / 255));
                    }
                    for (int x = 0; x <= 250; x += 5)
                    {
                        if (state != 3) { break; }
                        Thread.Sleep(10);
                        LogitechGSDK.LogiLedSetLighting((int)Math.Ceiling((double)(x * 100) / 255), 0, (int)Math.Ceiling((double)(250 * 100) / 255));
                    }
                    for (int x = 250; x >= 5; x -= 5)
                    {
                        if (state != 3) { break; }
                        Thread.Sleep(10);
                        LogitechGSDK.LogiLedSetLighting((int)Math.Ceiling((double)(250 * 100) / 255), 0, (int)Math.Ceiling((double)(x * 100) / 255));
                    }
                }
                Thread.Sleep(100);
                LogitechGSDK.LogiLedSetLighting((int)Math.Ceiling((double)(col.R * 100) / 255), (int)Math.Ceiling((double)(col.G * 100) / 255), (int)Math.Ceiling((double)(col.B * 100) / 255));
            }
        }

        static readonly object _transition = new object();
        private void transition(Corale.Colore.Core.Color col, bool forward)
        {
            lock (_transition)
            {
                for (uint c = 0; c < Corale.Colore.Razer.Keyboard.Constants.MaxColumns; c++)
                {
                    for (uint r = 0; r < Corale.Colore.Razer.Keyboard.Constants.MaxRows; r++)
                    {
                        var row = (forward) ? r : Corale.Colore.Razer.Keyboard.Constants.MaxRows - r - 1;
                        var colu = (forward) ? c : Corale.Colore.Razer.Keyboard.Constants.MaxColumns - c - 1;
                        Keyboard.Instance[row, colu] = Corale.Colore.Core.Color.FromSystemColor(col);
                    }
                    Thread.Sleep(15);
                }
            }
        }


        static readonly object _transitionConst = new object();
        private void transitionConst(Corale.Colore.Core.Color col1, Corale.Colore.Core.Color col2, bool forward, int speed)
        {
            lock (_transitionConst)
            {
                int i = 1;
                while (state == 6 || skyWatcher.Enabled == true)
                {
                    CTS.Token.ThrowIfCancellationRequested();
                    if (i == 1)
                    {
                        for (uint c = 0; c < Corale.Colore.Razer.Keyboard.Constants.MaxColumns; c++)
                        {
                            for (uint r = 0; r < Corale.Colore.Razer.Keyboard.Constants.MaxRows; r++)
                            {
                                if (state != 6) { break; }
                                var row = (forward) ? r : Corale.Colore.Razer.Keyboard.Constants.MaxRows - r - 1;
                                var colu = (forward) ? c : Corale.Colore.Razer.Keyboard.Constants.MaxColumns - c - 1;
                                try
                                {
                                    Keyboard.Instance[row, colu] = Corale.Colore.Core.Color.FromSystemColor(col1);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                }

                            }
                            Thread.Sleep(speed);
                        }
                        i = 2;
                    }
                    else if (i == 2)
                    {
                        for (uint c = 0; c < Corale.Colore.Razer.Keyboard.Constants.MaxColumns; c++)
                        {
                            for (uint r = 0; r < Corale.Colore.Razer.Keyboard.Constants.MaxRows; r++)
                            {
                                if (state != 6) { break; }
                                var row = (forward) ? r : Corale.Colore.Razer.Keyboard.Constants.MaxRows - r - 1;
                                var colu = (forward) ? c : Corale.Colore.Razer.Keyboard.Constants.MaxColumns - c - 1;
                                try
                                {
                                    Keyboard.Instance[row, colu] = Corale.Colore.Core.Color.FromSystemColor(col2);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                }
                            }
                            Thread.Sleep(speed);
                        }
                        i = 1;
                    }
                }
            }
        }


        //Handle Flash timers
        private int tick = 1;
        private int finaltick;
        private bool tickstart = false;
        private bool dpstickstart = false;
        System.Timers.Timer flashloop;
        System.Timers.Timer dpsloop;

        public void flashState(string type, int _maxtick, int _tickspeed)
        {
            if (Trigger == false)
            {
                Trigger = true;
                tickstart = true;
                if (DPSparse == true)
                {
                    updateState("static", btn_DPSLimitCol.BackColor, btn_defaultCol.BackColor);
                    flashloop = new System.Timers.Timer();
                    flashloop.Elapsed += (source, e) => { flashF(source, _maxtick, type); };
                    flashloop.Interval = _tickspeed;
                    flashloop.Enabled = true;
                }
                else if (state == 2)
                {
                    updateState("static", btn_emnityCol.BackColor, btn_defaultCol.BackColor);
                    flashloop = new System.Timers.Timer();
                    flashloop.Elapsed += (source, e) => { flashB(source, _maxtick, type); };
                    flashloop.Interval = _tickspeed;
                    flashloop.Enabled = true;
                }
                else if (state == 3)
                {
                    updateState("wave", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                    flashloop = new System.Timers.Timer();
                    flashloop.Elapsed += (source, e) => { flashC(source, _maxtick, type); };
                    flashloop.Interval = _tickspeed;
                    flashloop.Enabled = true;
                }
                else if (state == 4)
                {
                    updateState("breath", btn_raidEffectsA.BackColor, btn_raidEffectsB.BackColor);
                    flashloop = new System.Timers.Timer();
                    flashloop.Elapsed += (source, e) => { flashD(source, _maxtick, type); };
                    flashloop.Interval = _tickspeed;
                    flashloop.Enabled = true;
                }
                else if (state == 6)
                {
                    skyWatcher.Enabled = false;
                    setWeather(calculateWeather(currentZone));
                    flashloop = new System.Timers.Timer();
                    flashloop.Elapsed += (source, e) => { flashE(source, _maxtick, type); };
                    flashloop.Interval = _tickspeed;
                    flashloop.Enabled = true;
                }
                else
                {
                    updateState("static", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                    flashloop = new System.Timers.Timer();
                    flashloop.Elapsed += (source, e) => { flashA(source, _maxtick, type); };
                    flashloop.Interval = _tickspeed;
                    flashloop.Enabled = true;
                }
            }
        }
        static readonly object _flashDPSTimer = new object();
        private void flashDPSTimer(object sender, int currentdps)
        {
            lock(_flashDPSTimer)
            {
                if (chk_DPSLimit.Checked && dpstickstart == true)
                {
                    if (tick == 1)
                    {
                        updateState("static", btn_DPSLimitCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                    }
                    else if (tick == 2)
                    {
                        if (chk_enableEmnity.Checked)
                        {
                            updateState("static", btn_emnityCol.BackColor, btn_defaultCol.BackColor);
                            tick = 1;
                        }
                        else
                        {
                            updateState("static", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                            tick = 1;
                        }
                    }
                }
                else
                {
                    dpsloop.Stop();
                    dpstickstart = false;
                    dpsloop.Enabled = false;
                }
            }
        }

        private void flashA(object sender, int _maxtick, string type)
        {
            if (tickstart == true)
            {
                finaltick = _maxtick;
                tickstart = false;
            }

            if (finaltick != 0)
            {
                if (tick == 1)
                {
                    if (type == "trigger")
                    {
                        updateState("static", btn_triggerCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "timer")
                    {
                        updateState("static", btn_timerCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "say")
                    {
                        updateState("static", btn_sayCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "tell")
                    {
                        updateState("static", btn_tellCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "yell")
                    {
                        updateState("static", btn_yellCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "shout")
                    {
                        updateState("static", btn_shoutCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "party")
                    {
                        updateState("static", btn_partyCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "alliance")
                    {
                        updateState("static", btn_allianceCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "fc")
                    {
                        updateState("static", btn_fcCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls1")
                    {
                        updateState("static", btn_ls1Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls2")
                    {
                        updateState("static", btn_ls2Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls3")
                    {
                        updateState("static", btn_ls3Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls4")
                    {
                        updateState("static", btn_ls4Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls5")
                    {
                        updateState("static", btn_ls5Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls6")
                    {
                        updateState("static", btn_ls6Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls7")
                    {
                        updateState("static", btn_ls7Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls8")
                    {
                        updateState("static", btn_ls8Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }

                }
                else if (tick == 2)
                {
                    updateState("static", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                    tick = 1;
                    finaltick--;
                }
            }
            else if (finaltick == 0)
            {
                flashloop.Enabled = false;
                Trigger = false;
                tick = 1;
            }
        }

        private void flashB(object sender, int _maxtick, string type)
        {
            if (tickstart == true)
            {
                finaltick = _maxtick;
                tickstart = false;
            }
            if (finaltick != 0)
            {
                if (tick == 1)
                {
                    if (type == "trigger")
                    {
                        updateState("static", btn_triggerCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "timer")
                    {
                        updateState("static", btn_timerCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "say")
                    {
                        updateState("static", btn_sayCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "tell")
                    {
                        updateState("static", btn_tellCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "yell")
                    {
                        updateState("static", btn_yellCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "shout")
                    {
                        updateState("static", btn_shoutCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "party")
                    {
                        updateState("static", btn_partyCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "alliance")
                    {
                        updateState("static", btn_allianceCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "fc")
                    {
                        updateState("static", btn_fcCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls1")
                    {
                        updateState("static", btn_ls1Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls2")
                    {
                        updateState("static", btn_ls2Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls3")
                    {
                        updateState("static", btn_ls3Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls4")
                    {
                        updateState("static", btn_ls4Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls5")
                    {
                        updateState("static", btn_ls5Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls6")
                    {
                        updateState("static", btn_ls6Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls7")
                    {
                        updateState("static", btn_ls7Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls8")
                    {
                        updateState("static", btn_ls8Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                }
                else if (tick == 2)
                {
                    updateState("static", btn_emnityCol.BackColor, btn_defaultCol.BackColor);
                    tick = 1;
                    finaltick--;
                }
            }
            else if (finaltick == 0)
            {
                flashloop.Enabled = false;
                Trigger = false;
                tick = 1;
            }
        }

        private void flashC(object sender, int _maxtick, string type)
        {
            if (tickstart == true)
            {
                finaltick = _maxtick;
                tickstart = false;
            }

            if (finaltick != 0)
            {
                if (tick == 1)
                {
                    if (type == "trigger")
                    {
                        updateState("static", btn_triggerCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "timer")
                    {
                        updateState("static", btn_timerCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "say")
                    {
                        updateState("static", btn_sayCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "tell")
                    {
                        updateState("static", btn_tellCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "yell")
                    {
                        updateState("static", btn_yellCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "shout")
                    {
                        updateState("static", btn_shoutCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "party")
                    {
                        updateState("static", btn_partyCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "alliance")
                    {
                        updateState("static", btn_allianceCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "fc")
                    {
                        updateState("static", btn_fcCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls1")
                    {
                        updateState("static", btn_ls1Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls2")
                    {
                        updateState("static", btn_ls2Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls3")
                    {
                        updateState("static", btn_ls3Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls4")
                    {
                        updateState("static", btn_ls4Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls5")
                    {
                        updateState("static", btn_ls5Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls6")
                    {
                        updateState("static", btn_ls6Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls7")
                    {
                        updateState("static", btn_ls7Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls8")
                    {
                        updateState("static", btn_ls8Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }

                }
                else if (tick == 2)
                {
                    updateState("wave", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                    tick = 1;
                    finaltick--;
                }
            }
            else if (finaltick == 0)
            {
                flashloop.Enabled = false;
                Trigger = false;
                tick = 1;
            }
        }

        private void flashD(object sender, int _maxtick, string type)
        {
            if (tickstart == true)
            {
                finaltick = _maxtick;
                tickstart = false;
            }

            if (finaltick != 0)
            {
                if (tick == 1)
                {
                    if (type == "trigger")
                    {
                        updateState("static", btn_triggerCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "timer")
                    {
                        updateState("static", btn_timerCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "say")
                    {
                        updateState("static", btn_sayCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "tell")
                    {
                        updateState("static", btn_tellCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "yell")
                    {
                        updateState("static", btn_yellCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "shout")
                    {
                        updateState("static", btn_shoutCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "party")
                    {
                        updateState("static", btn_partyCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "alliance")
                    {
                        updateState("static", btn_allianceCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "fc")
                    {
                        updateState("static", btn_fcCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls1")
                    {
                        updateState("static", btn_ls1Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls2")
                    {
                        updateState("static", btn_ls2Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls3")
                    {
                        updateState("static", btn_ls3Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls4")
                    {
                        updateState("static", btn_ls4Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls5")
                    {
                        updateState("static", btn_ls5Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls6")
                    {
                        updateState("static", btn_ls6Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls7")
                    {
                        updateState("static", btn_ls7Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls8")
                    {
                        updateState("static", btn_ls8Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }

                }
                else if (tick == 2)
                {
                    updateState("breath", btn_raidEffectsA.BackColor, btn_raidEffectsB.BackColor);
                    tick = 1;
                    finaltick--;
                }
            }
            else if (finaltick == 0)
            {
                flashloop.Enabled = false;
                Trigger = false;
                tick = 1;
            }
        }

        private void flashE(object sender, int _maxtick, string type)
        {
            if (tickstart == true)
            {
                finaltick = _maxtick;
                tickstart = false;
            }

            if (finaltick != 0)
            {
                if (tick == 1)
                {
                    if (type == "trigger")
                    {
                        updateState("static", btn_triggerCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "timer")
                    {
                        updateState("static", btn_timerCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "say")
                    {
                        updateState("static", btn_sayCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "tell")
                    {
                        updateState("static", btn_tellCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "yell")
                    {
                        updateState("static", btn_yellCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "shout")
                    {
                        updateState("static", btn_shoutCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "party")
                    {
                        updateState("static", btn_partyCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "alliance")
                    {
                        updateState("static", btn_allianceCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "fc")
                    {
                        updateState("static", btn_fcCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls1")
                    {
                        updateState("static", btn_ls1Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls2")
                    {
                        updateState("static", btn_ls2Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls3")
                    {
                        updateState("static", btn_ls3Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls4")
                    {
                        updateState("static", btn_ls4Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls5")
                    {
                        updateState("static", btn_ls5Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls6")
                    {
                        updateState("static", btn_ls6Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls7")
                    {
                        updateState("static", btn_ls7Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls8")
                    {
                        updateState("static", btn_ls8Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }

                }
                else if (tick == 2)
                {
                    setWeather(calculateWeather(currentZone));
                    tick = 1;
                    finaltick--;
                }
            }
            else if (finaltick == 0)
            {
                flashloop.Enabled = false;
                skyWatcher.Enabled = true;
                state = 6;
                Trigger = false;
                tick = 1;
            }
        }

        private void flashF(object sender, int _maxtick, string type)
        {
            if (tickstart == true)
            {
                finaltick = _maxtick;
                tickstart = false;
            }

            if (finaltick != 0)
            {
                if (tick == 1)
                {
                    if (type == "trigger")
                    {
                        updateState("static", btn_triggerCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "timer")
                    {
                        updateState("static", btn_timerCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "say")
                    {
                        updateState("static", btn_sayCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "tell")
                    {
                        updateState("static", btn_tellCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "yell")
                    {
                        updateState("static", btn_yellCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "shout")
                    {
                        updateState("static", btn_shoutCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "party")
                    {
                        updateState("static", btn_partyCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "alliance")
                    {
                        updateState("static", btn_allianceCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "fc")
                    {
                        updateState("static", btn_fcCol.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls1")
                    {
                        updateState("static", btn_ls1Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls2")
                    {
                        updateState("static", btn_ls2Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls3")
                    {
                        updateState("static", btn_ls3Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls4")
                    {
                        updateState("static", btn_ls4Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls5")
                    {
                        updateState("static", btn_ls5Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls6")
                    {
                        updateState("static", btn_ls6Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls7")
                    {
                        updateState("static", btn_ls7Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }
                    else if (type == "ls8")
                    {
                        updateState("static", btn_ls8Col.BackColor, btn_defaultCol.BackColor);
                        tick = 2;
                        finaltick--;
                    }

                }
                else if (tick == 2)
                {
                    updateState("static", btn_DPSLimitCol.BackColor, btn_defaultCol.BackColor);
                    tick = 1;
                    finaltick--;
                }
            }
            else if (finaltick == 0)
            {
                flashloop.Enabled = false;
                Trigger = false;
                tick = 1;
            }
        }


        //Handle in-game weather parsing
        private int calculateWeatherVar()
        {
            var unixSeconds = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            
            // Get Eorzea hour for weather start
            var bell = unixSeconds / 175;

            // Do the magic 'cause for calculations 16:00 is 0, 00:00 is 8 and 08:00 is 16
            var increment = ((bell + 8) - (bell % 8)) % 24;
            
            // Take Eorzea days since unix epoch
            var totalDays = unixSeconds / 4200;

            totalDays = ((int)(uint)totalDays << 32) >> 0; // Convert to uint

            // 0x64 = 100
            var calcBase = totalDays * 100 + increment;
            
            // 0xB = 11
            var step1 = (calcBase << 11) ^ calcBase;
            var step2 = ((int)(uint)step1 >> 8) ^ step1;

            // 0x64 = 100
            return step2 % 100;
        }


        //Handle ACT events
        void oFormActMain_AfterCombatAction(bool isImport, CombatActionEventArgs actionInfo)
        {
            throw new NotImplementedException();
        }

        void oFormActMain_OnCombatStart(bool isImport, CombatToggleEventArgs actionInfo)
        {
            if (ChromaReady == true && chk_enableEmnity.Checked && state != 3)
            {
                inCombat = true;
                ActiveEnconter = actionInfo.encounter;
                if (dpstickstart == true)
                {
                    dpsloop.Stop();
                    dpstickstart = false;
                    dpsloop.Enabled = false;
                }
                if (state == 6)
                {
                    skyWatcher.Enabled = false;
                    updateState("transition", btn_emnityCol.BackColor, btn_defaultCol.BackColor, true);
                    state = 6;
                }
                else if (state == 4)
                {
                    updateState("transition", btn_emnityCol.BackColor, btn_defaultCol.BackColor, true);
                    state = 4;
                }
                else
                {
                    updateState("transition", btn_emnityCol.BackColor, btn_defaultCol.BackColor, true);
                    state = 2;
                }
            }
        }

        void oFormActMain_OnCombatEnd(bool isImport, CombatToggleEventArgs actionInfo)
        {
            inCombat = false;
            ActiveEnconter = null;
            if (dpstickstart == true)
            {
                dpsloop.Stop();
                dpstickstart = false;
                dpsloop.Enabled = false;
            }

            if (ChromaReady == true && chk_enableEmnity.Checked)
            {
                if (state == 6)
                {
                    setWeather(calculateWeather(currentZone));
                    skyWatcher.Enabled = true;
                    state = 6;
                }
                else if (state == 4)
                {
                    updateState("breath", btn_raidEffectsA.BackColor, btn_raidEffectsB.BackColor);
                    skyWatcher.Enabled = false;
                    state = 4;
                }
                else
                {
                    updateState("transition", btn_defaultCol.BackColor, btn_defaultCol.BackColor, true);
                    skyWatcher.Enabled = false;
                    state = 1;
                }
            }
        }

        private int reachDPS = 1;
        void oFormActMain_BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            //Init
            string log = logInfo.logLine;
            int chattick = 4;
            int chatspeed = 300;

            //Zone Engine
            if (currentZone != logInfo.detectedZone)
            {
                currentZone = logInfo.detectedZone;

                if (RzPulse == true)
                {
                    CTS.Cancel();
                    RzPulse = false;
                }

                //Raid Effects Mode
                if (chk_raidEffects.Checked && raidZoneList.Contains(currentZone))
                {
                    if (state != 4)
                    {
                        state = 4;
                        skyWatcher.Enabled = false;
                        updateState("breath", btn_raidEffectsA.BackColor, btn_raidEffectsB.BackColor);
                    }
                    else
                    {
                        updateState("static", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                        state = 1;
                        currentZone = "";
                    }
                }

                //Gold Saucer Vegas Mode
                else if (chk_GoldSaucerVegas.Checked && (currentZone == "The Golden Saucer" || currentZone == "Chocobo Square" || currentZone == "Unknown Zone (184)"))
                {
                    skyWatcher.Enabled = false;
                    updateState("wave", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                    state = 3;
                }
                

                //Reactive Weather
                else if (chk_reactiveWeather.Checked)
                {
                    state = 6;
                    setWeather(calculateWeather(currentZone));
                    skyWatcher.Enabled = true;
                }

                //Return to Default
                else
                {
                    updateState("static", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                    state = 1;
                    CTS.Cancel();
                    skyWatcher.Enabled = false;
                }
            }

            //DPS Threshold
            if (chk_DPSLimit.Checked && inCombat == true)
            {
                int currentDPS = Convert.ToInt32(ActiveEnconter.GetCombatant("YOU").EncDPS);
                int limitDPS = Convert.ToInt32(txt_DPSlimit.Value);
                int notifyDPS = Convert.ToInt32(txt_DPSNotify.Value);
                int min = 120;
                int max = 800;

                int tickspeed = ((currentDPS - notifyDPS) * (min - max) / (limitDPS - notifyDPS)) + max;
                                
                if (currentDPS >= limitDPS)
                {
                    if (reachDPS == 1 || reachDPS == 2)
                    {
                        Debug.WriteLine(reachDPS);
                        DPSparse = true;
                        reachDPS = 3;
                        updateState("static", btn_DPSLimitCol.BackColor, btn_defaultCol.BackColor);
                        dpsloop.Stop();
                        dpstickstart = false;
                        dpsloop.Enabled = false;
                    }
                }
                else if ((currentDPS >= notifyDPS && currentDPS < limitDPS) && notifyDPS >= 0)
                {
                    if (dpstickstart == false)
                    {
                        DPSparse = true;
                        reachDPS = 2;
                        if (chk_enableEmnity.Checked)
                        {
                            new Task(() =>
                            {
                                dpstickstart = true;
                                updateState("static", btn_emnityCol.BackColor, btn_defaultCol.BackColor);
                                dpsloop = new System.Timers.Timer();
                                dpsloop.Elapsed += (source, e) => { flashDPSTimer(source, currentDPS); };
                                dpsloop.Interval = tickspeed;
                                dpsloop.Enabled = true;
                            }).Start();
                        }
                        else
                        {
                            new Task(() =>
                            {
                                dpstickstart = true;
                                updateState("static", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                                dpsloop = new System.Timers.Timer();
                                dpsloop.Elapsed += (source, e) => { flashDPSTimer(source, currentDPS); };
                                dpsloop.Interval = tickspeed;
                                dpsloop.Enabled = true;
                            }).Start();
                        }
                    }
                    else
                    {
                        dpsloop.Interval = tickspeed;
                    }
                }
                else
                {
                    if (reachDPS == 2 || reachDPS == 3)
                    {
                        if (chk_enableEmnity.Checked)
                        {
                            updateState("static", btn_emnityCol.BackColor, btn_defaultCol.BackColor);
                        }
                        else
                        {
                            updateState("static", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
                        }
                        reachDPS = 1;
                        DPSparse = false;
                        dpsloop.Stop();
                        dpstickstart = false;
                        dpsloop.Enabled = false;
                    }
                }     
            }

            //Chat Engine
            string receipt = log.Split('.')[1];
            string author = receipt.Split(']')[0];
            string messagetype = log.Substring(20).Split(':')[0];
            
            if (author == "000")
            {
                
                //SAY
                if (messagetype == "0a")
                {
                    if (ChromaReady == true && chk_say.Checked)
                    {
                        flashState("say", chattick, chatspeed);
                    }
                }
                //TELL
               else  if (messagetype == "0d")
                {
                    if (ChromaReady == true && chk_tell.Checked)
                    {
                        flashState("tell", chattick, chatspeed);
                    }
                }
                //YELL
                else if (messagetype == "1e")
                {
                    if (ChromaReady == true && chk_yell.Checked)
                    {
                        flashState("yell", chattick, chatspeed);
                    }
                }
                //PARTY
                else if (messagetype == "0e")
                {
                    if (ChromaReady == true && chk_party.Checked)
                    {
                        flashState("party", chattick, chatspeed);
                    }
                }
                //ALLIANCE
                else if (messagetype == "0f")
                {
                    if (ChromaReady == true && chk_alliance.Checked)
                    {
                        flashState("alliance", chattick, chatspeed);
                    }
                }
                //FC
                else if (messagetype == "18")
                {
                    if (ChromaReady == true && chk_fc.Checked)
                    {
                        flashState("fc", chattick, chatspeed);
                    }
                }
                //SHOUT
                else if (messagetype == "0b")
                {
                    if (ChromaReady == true && chk_shout.Checked)
                    {
                        flashState("shout", chattick, chatspeed);
                    }
                }
                //LS1
                else if (messagetype == "10")
                {
                    if (ChromaReady == true && chk_ls1.Checked)
                    {
                        flashState("ls1", chattick, chatspeed);
                    }
                }
                //LS2
                else if (messagetype == "11")
                {
                    if (ChromaReady == true && chk_ls2.Checked)
                    {
                        flashState("ls2", chattick, chatspeed);
                    }
                }
                //LS3
                else if (messagetype == "12")
                {
                    if (ChromaReady == true && chk_ls3.Checked)
                    {
                        flashState("ls3", chattick, chatspeed);
                    }
                }
                //LS4
                else if (messagetype == "13")
                {
                    if (ChromaReady == true && chk_ls4.Checked)
                    {
                        flashState("ls4", chattick, chatspeed);
                    }
                }
                //LS5
                else if (messagetype == "14")
                {
                    if (ChromaReady == true && chk_ls5.Checked)
                    {
                        flashState("ls5", chattick, chatspeed);
                    }
                }
                //LS6
                else if (messagetype == "15")
                {
                    if (ChromaReady == true && chk_ls6.Checked)
                    {
                        flashState("ls6", chattick, chatspeed);
                    }
                }
                //LS7
                else if (messagetype == "16")
                {
                    if (ChromaReady == true && chk_ls7.Checked)
                    {
                        flashState("ls7", chattick, chatspeed);
                    }
                }
                //LS8
                else if (messagetype == "17")
                {
                    if (ChromaReady == true && chk_ls8.Checked)
                    {
                        flashState("ls8", chattick, chatspeed);
                    }
                }
            }

            //Custom Triggers
            string loglineA = log.Substring(23);
            
            if (customTriggers.Any(str => loglineA.Contains(str)))
            {
                if (ChromaReady == true && chk_enableTriggers.Checked)
                {
                    flashState("trigger", maxtick, tickspeed);
                }
                
            }
        }


        //Check for automatic updates
        void oFormActMain_UpdateCheckClicked()
        {

            int pluginId = 68;
            try
            {
                DateTime localDate = ActGlobals.oFormActMain.PluginGetSelfDateUtc(this);
                DateTime remoteDate = ActGlobals.oFormActMain.PluginGetRemoteDateUtc(pluginId);

                string currentVersion = "1.1.4".Replace(".", string.Empty); //UPDATE ME
                string newVersion = currentVersion;
                var webRequest = WebRequest.Create(@"http://thejourneynetwork.net/chromatics/update/version.txt");

                using (var response = webRequest.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                {
                    string newVersionA = reader.ReadToEnd();
                    newVersion = newVersionA.Replace(".", string.Empty);
                }

                int cV = int.Parse(currentVersion);
                int nV = int.Parse(newVersion);

                if (nV > cV)
                {
                    DialogResult result = MessageBox.Show("There is an updated version of Chromatics.  Update it now?\n\n(If there is an update to ACT, you should click No and update ACT first.)", "New Version", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {

                        ActPluginData pluginData = ActGlobals.oFormActMain.PluginGetSelfData(this);
                        string updatedFile = Path.GetDirectoryName(pluginData.pluginFile.FullName);

                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = updatedFile + @"\lib\updater.exe";
                        startInfo.Arguments = "\"" + updatedFile + "\"";
                        Process.Start(startInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                ActGlobals.oFormActMain.WriteExceptionLog(ex, "Plugin Update Check");
            }
        }


        //Handle weather events
        void weatherLoop()
        {
            if (skyWatcher.Enabled == true)
            {
                if (currentWeather != calculateWeather(currentZone)[0])
                {
                    currentWeather = calculateWeather(currentZone)[0];
                    setWeather(calculateWeather(currentZone));
                }
            }
        }

        //Handle spell timer events
        void oFormSpellTimers_OnSpellTimerExpire(TimerFrame actionInfo)
        {
            if (ChromaReady == true && chk_enableTimers.Checked && timerevent == "Expire")
            {
                flashState("timer", timercount, tickspeed);
            }
        }

        void oFormSpellTimers_OnSpellTimerWarning(TimerFrame actionInfo)
        {
            if (ChromaReady == true && chk_enableTimers.Checked && timerevent == "Warning")
            {
                flashState("timer", timercount, tickspeed);
            }
        }

        void oFormSpellTimers_OnSpellTimerRemoved(TimerFrame actionInfo)
        {
            if (ChromaReady == true && chk_enableTimers.Checked && timerevent == "Removed")
            {
                flashState("timer", timercount, tickspeed);
            }
        }

        private void btn_restoreDefaults_Click(object sender, EventArgs e)
        {
            RestoreDefaults();
        }


        //Weather dictionary
        private string[] calculateWeather(string ActiveZone)
        {
            var weatherVariance = calculateWeatherVar();
            string[] weather = new string[3];
            
            if (ActiveZone == "Limsa Lominsa Lower Decks" || ActiveZone == "Limsa Lominsa Upper Decks")
            {
                if (weatherVariance >= 0 && weatherVariance < 20) { weather[0] = "Clouds"; weather[1] = "Limsa Lominsa"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 20 && weatherVariance < 50) { weather[0] = "Clear Skies"; weather[1] = "Limsa Lominsa"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 50 && weatherVariance < 80) { weather[0] = "Fair Skies"; weather[1] = "Limsa Lominsa"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 80 && weatherVariance < 90) { weather[0] = "Fog"; weather[1] = "Limsa Lominsa"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 90 && weatherVariance < 100) { weather[0] = "Rain"; weather[1] = "Limsa Lominsa"; weather[2] = "La Noscea"; }
                
            }
            else if (ActiveZone == "Mist")
            {
                if (weatherVariance >= 0 && weatherVariance < 20) { weather[0] = "Clouds"; weather[1] = "Mist"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 20 && weatherVariance < 50) { weather[0] = "Clear Skies"; weather[1] = "Mist"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 50 && weatherVariance < 80) { weather[0] = "Fair Skies"; weather[1] = "Mist"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 80 && weatherVariance < 90) { weather[0] = "Fog"; weather[1] = "Mist"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 90 && weatherVariance < 100) { weather[0] = "Rain"; weather[1] = "Mist"; weather[2] = "La Noscea"; }
            }
            else if (ActiveZone == "Middle La Noscea")
            {
                if (weatherVariance >= 0 && weatherVariance < 20) { weather[0] = "Clouds"; weather[1] = "Middle La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 20 && weatherVariance < 50) { weather[0] = "Clear Skies"; weather[1] = "Middle La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 50 && weatherVariance < 70) { weather[0] = "Fair Skies"; weather[1] = "Middle La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 70 && weatherVariance < 80) { weather[0] = "Wind"; weather[1] = "Middle La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 80 && weatherVariance < 90) { weather[0] = "Fog"; weather[1] = "Middle La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 90 && weatherVariance < 100) { weather[0] = "Rain"; weather[1] = "Middle La Noscea"; weather[2] = "La Noscea"; }
            }
            else if (ActiveZone == "Lower La Noscea")
            {
                if (weatherVariance >= 0 && weatherVariance < 20) { weather[0] = "Clouds"; weather[1] = "Lower La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 20 && weatherVariance < 50) { weather[0] = "Clear Skies"; weather[1] = "Lower La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 50 && weatherVariance < 70) { weather[0] = "Fair Skies"; weather[1] = "Lower La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 70 && weatherVariance < 80) { weather[0] = "Wind"; weather[1] = "Lower La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 80 && weatherVariance < 90) { weather[0] = "Fog"; weather[1] = "Lower La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 90 && weatherVariance < 100) { weather[0] = "Rain"; weather[1] = "Lower La Noscea"; weather[2] = "La Noscea"; }
            }
            else if (ActiveZone == "Eastern La Noscea")
            {
                if (weatherVariance >= 0 && weatherVariance < 5) { weather[0] = "Fog"; weather[1] = "Eastern La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 5 && weatherVariance < 50) { weather[0] = "Clear Skies"; weather[1] = "Eastern La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 70 && weatherVariance < 80) { weather[0] = "Fair Skies"; weather[1] = "Eastern La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 80 && weatherVariance < 90) { weather[0] = "Clouds"; weather[1] = "Eastern La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 90 && weatherVariance < 95) { weather[0] = "Rain"; weather[1] = "Eastern La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 95 && weatherVariance < 100) { weather[0] = "Showers"; weather[1] = "Eastern La Noscea"; weather[2] = "La Noscea"; }
            }
            else if (ActiveZone == "Western La Noscea")
            {
                if (weatherVariance >= 0 && weatherVariance < 10) { weather[0] = "Fog"; weather[1] = "Western La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 10 && weatherVariance < 40) { weather[0] = "Clear Skies"; weather[1] = "Western La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 40 && weatherVariance < 60) { weather[0] = "Fair Skies"; weather[1] = "Western La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 60 && weatherVariance < 80) { weather[0] = "Clouds"; weather[1] = "Western La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 80 && weatherVariance < 90) { weather[0] = "Wind"; weather[1] = "Western La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 90 && weatherVariance < 100) { weather[0] = "Gales"; weather[1] = "Western La Noscea"; weather[2] = "La Noscea"; }
            }
            else if (ActiveZone == "Upper La Noscea")
            {
                if (weatherVariance >= 0 && weatherVariance < 30) { weather[0] = "Clear Skies"; weather[1] = "Upper La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 30 && weatherVariance < 50) { weather[0] = "Fair Skies"; weather[1] = "Upper La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 50 && weatherVariance < 70) { weather[0] = "Clouds"; weather[1] = "Upper La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 70 && weatherVariance < 80) { weather[0] = "Fog"; weather[1] = "Upper La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 80 && weatherVariance < 90) { weather[0] = "Thunder"; weather[1] = "Upper La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 90 && weatherVariance < 100) { weather[0] = "Thunderstorms"; weather[1] = "Upper La Noscea"; weather[2] = "La Noscea"; }
            }
            else if (ActiveZone == "Outer La Noscea")
            {
                if (weatherVariance >= 0 && weatherVariance < 30) { weather[0] = "Clear Skies"; weather[1] = "Outer La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 30 && weatherVariance < 50) { weather[0] = "Fair Skies"; weather[1] = "Outer La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 50 && weatherVariance < 70) { weather[0] = "Clouds"; weather[1] = "Outer La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 70 && weatherVariance < 85) { weather[0] = "Fog"; weather[1] = "Outer La Noscea"; weather[2] = "La Noscea"; }
                else if (weatherVariance >= 85 && weatherVariance < 100) { weather[0] = "Rain"; weather[1] = "Outer La Noscea"; weather[2] = "La Noscea"; }
            }
            else if (ActiveZone == "New Gridania" || ActiveZone == "Old Gridania")
            {
                if (weatherVariance >= 0 && weatherVariance < 5) { weather[0] = "Rain"; weather[1] = "Gridania"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 5 && weatherVariance < 20) { weather[0] = "Rain"; weather[1] = "Gridania"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 20 && weatherVariance < 30) { weather[0] = "Fog"; weather[1] = "Gridania"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 30 && weatherVariance < 40) { weather[0] = "Clouds"; weather[1] = "Gridania"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 40 && weatherVariance < 55) { weather[0] = "Fair Skies"; weather[1] = "Gridania"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 55 && weatherVariance < 85) { weather[0] = "Clear Skies"; weather[1] = "Gridania"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 85 && weatherVariance < 100) { weather[0] = "Fair Skies"; weather[1] = "Gridania"; weather[2] = "The Black Shroud"; }
            }
            else if (ActiveZone == "Central Shroud")
            {
                if (weatherVariance >= 0 && weatherVariance < 5) { weather[0] = "Thunder"; weather[1] = "Central Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 5 && weatherVariance < 20) { weather[0] = "Rain"; weather[1] = "Central Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 20 && weatherVariance < 30) { weather[0] = "Fog"; weather[1] = "Central Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 30 && weatherVariance < 40) { weather[0] = "Clouds"; weather[1] = "Central Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 40 && weatherVariance < 55) { weather[0] = "Fair Skies"; weather[1] = "Central Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 55 && weatherVariance < 85) { weather[0] = "Clear Skies"; weather[1] = "Central Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 85 && weatherVariance < 100) { weather[0] = "Fair Skies"; weather[1] = "Central Shroud"; weather[2] = "The Black Shroud"; }
            }
            else if (ActiveZone == "East Shroud")
            {
                if (weatherVariance >= 0 && weatherVariance < 5) { weather[0] = "Thunder"; weather[1] = "East Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 5 && weatherVariance < 20) { weather[0] = "Rain"; weather[1] = "East Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 20 && weatherVariance < 30) { weather[0] = "Fog"; weather[1] = "East Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 30 && weatherVariance < 40) { weather[0] = "Clouds"; weather[1] = "East Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 40 && weatherVariance < 55) { weather[0] = "Fair Skies"; weather[1] = "East Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 55 && weatherVariance < 85) { weather[0] = "Clear Skies"; weather[1] = "East Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 85 && weatherVariance < 100) { weather[0] = "Fair Skies"; weather[1] = "East Shroud"; weather[2] = "The Black Shroud"; }
            }
            else if (ActiveZone == "South Shroud")
            {
                if (weatherVariance >= 0 && weatherVariance < 5) { weather[0] = "Fog"; weather[1] = "South Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 5 && weatherVariance < 10) { weather[0] = "Thunderstorms"; weather[1] = "South Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 10 && weatherVariance < 25) { weather[0] = "Thunder"; weather[1] = "South Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 25 && weatherVariance < 30) { weather[0] = "Fog"; weather[1] = "South Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 30 && weatherVariance < 40) { weather[0] = "Clouds"; weather[1] = "South Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 40 && weatherVariance < 70) { weather[0] = "Fair Skies"; weather[1] = "South Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 70 && weatherVariance < 100) { weather[0] = "Clear Skies"; weather[1] = "South Shroud"; weather[2] = "The Black Shroud"; }
            }
            else if (ActiveZone == "North Shroud")
            {
                if (weatherVariance >= 0 && weatherVariance < 5) { weather[0] = "Fog"; weather[1] = "North Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 5 && weatherVariance < 10) { weather[0] = "Shwowers"; weather[1] = "North Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 10 && weatherVariance < 25) { weather[0] = "Rain"; weather[1] = "North Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 25 && weatherVariance < 30) { weather[0] = "Fog"; weather[1] = "North Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 30 && weatherVariance < 40) { weather[0] = "Clouds"; weather[1] = "North Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 40 && weatherVariance < 70) { weather[0] = "Fair Skies"; weather[1] = "North Shroud"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 70 && weatherVariance < 100) { weather[0] = "Clear Skies"; weather[1] = "North Shroud"; weather[2] = "The Black Shroud"; }
            }
            else if (ActiveZone == "The Lavender Beds")
            {
                if (weatherVariance >= 0 && weatherVariance < 5) { weather[0] = "Clouds"; weather[1] = "The Lavender Beds"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 5 && weatherVariance < 20) { weather[0] = "Rain"; weather[1] = "The Lavender Beds"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 20 && weatherVariance < 30) { weather[0] = "Fog"; weather[1] = "The Lavender Beds"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 30 && weatherVariance < 40) { weather[0] = "Clouds"; weather[1] = "The Lavender Beds"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 40 && weatherVariance < 55) { weather[0] = "Fair Skies"; weather[1] = "The Lavender Beds"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 55 && weatherVariance < 85) { weather[0] = "Clear Skies"; weather[1] = "The Lavender Beds"; weather[2] = "The Black Shroud"; }
                else if (weatherVariance >= 85 && weatherVariance < 100) { weather[0] = "Fair Skies"; weather[1] = "The Lavender Beds"; weather[2] = "The Black Shroud"; }
            }
            else if (ActiveZone == "Ul'dah - Steps of Nald" || ActiveZone == "Ul'dah - Steps of Thal")
            {
                if (weatherVariance >= 0 && weatherVariance < 40) { weather[0] = "Clear Skies"; weather[1] = "Ul'dah"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 40 && weatherVariance < 60) { weather[0] = "Fair Skies"; weather[1] = "Ul'dah"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 60 && weatherVariance < 85) { weather[0] = "Clouds"; weather[1] = "Ul'dah"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 85 && weatherVariance < 95) { weather[0] = "Fog"; weather[1] = "Ul'dah"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 95 && weatherVariance < 100) { weather[0] = "Rain"; weather[1] = "Ul'dah"; weather[2] = "Thanalan"; }
            }
            else if (ActiveZone == "Western Thanalan")
            {
                if (weatherVariance >= 0 && weatherVariance < 40) { weather[0] = "Clear Skies"; weather[1] = "Western Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 40 && weatherVariance < 60) { weather[0] = "Fair Skies"; weather[1] = "Western Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 60 && weatherVariance < 85) { weather[0] = "Clouds"; weather[1] = "Western Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 85 && weatherVariance < 95) { weather[0] = "Fog"; weather[1] = "Western Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 95 && weatherVariance < 100) { weather[0] = "Rain"; weather[1] = "Western Thanalan"; weather[2] = "Thanalan"; }
            }
            else if (ActiveZone == "Central Thanalan")
            {
                if (weatherVariance >= 0 && weatherVariance < 15) { weather[0] = "Dust Storms"; weather[1] = "Central Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 15 && weatherVariance < 55) { weather[0] = "Clear Skies"; weather[1] = "Central Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 55 && weatherVariance < 75) { weather[0] = "Fair Skies"; weather[1] = "Central Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 75 && weatherVariance < 85) { weather[0] = "Clouds"; weather[1] = "Central Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 85 && weatherVariance < 95) { weather[0] = "Fog"; weather[1] = "Central Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 95 && weatherVariance < 100) { weather[0] = "Rain"; weather[1] = "Central Thanalan"; weather[2] = "Thanalan"; }
            }
            else if (ActiveZone == "Eastern Thanalan")
            {
                if (weatherVariance >= 0 && weatherVariance < 40) { weather[0] = "Clear Skies"; weather[1] = "Eastern Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 40 && weatherVariance < 60) { weather[0] = "Fair Skies"; weather[1] = "Eastern Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 60 && weatherVariance < 70) { weather[0] = "Clouds"; weather[1] = "Eastern Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 70 && weatherVariance < 80) { weather[0] = "Fog"; weather[1] = "Eastern Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 80 && weatherVariance < 85) { weather[0] = "Rain"; weather[1] = "Eastern Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 85 && weatherVariance < 100) { weather[0] = "Showers"; weather[1] = "Eastern Thanalan"; weather[2] = "Thanalan"; }
            }
            else if (ActiveZone == "Southern Thanalan")
            {
                if (weatherVariance >= 0 && weatherVariance < 20) { weather[0] = "Heat Waves"; weather[1] = "Southern Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 20 && weatherVariance < 60) { weather[0] = "Clear Skies"; weather[1] = "Southern Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 60 && weatherVariance < 80) { weather[0] = "Fair Skies"; weather[1] = "Southern Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 80 && weatherVariance < 90) { weather[0] = "Clouds"; weather[1] = "Southern Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 90 && weatherVariance < 100) { weather[0] = "Fog"; weather[1] = "Southern Thanalan"; weather[2] = "Thanalan"; }
            }
            else if (ActiveZone == "Northern Thanalan")
            {
                if (weatherVariance >= 0 && weatherVariance < 5) { weather[0] = "Clear Skies"; weather[1] = "Northern Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 5 && weatherVariance < 20) { weather[0] = "Fair Skies"; weather[1] = "Northern Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 20 && weatherVariance < 50) { weather[0] = "Clouds"; weather[1] = "Northern Thanalan"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 50 && weatherVariance < 100) { weather[0] = "Fog"; weather[1] = "Northern Thanalan"; weather[2] = "Thanalan"; }
            }
            else if (ActiveZone == "The Goblet")
            {
                if (weatherVariance >= 0 && weatherVariance < 40) { weather[0] = "Clear Skies"; weather[1] = "The Goblet"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 40 && weatherVariance < 60) { weather[0] = "Fair Skies"; weather[1] = "The Goblet"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 60 && weatherVariance < 85) { weather[0] = "Clouds"; weather[1] = "The Goblet"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 85 && weatherVariance < 95) { weather[0] = "Fog"; weather[1] = "The Goblet"; weather[2] = "Thanalan"; }
                else if (weatherVariance >= 95 && weatherVariance < 100) { weather[0] = "Rain"; weather[1] = "The Goblet"; weather[2] = "Thanalan"; }
            }
            else if (ActiveZone == "The Holy See Of Ishgard: Foundation" || ActiveZone == "The Holy See Of Ishgard: The Pillars")
            {
                if (weatherVariance >= 0 && weatherVariance < 60) { weather[0] = "Snow"; weather[1] = "Ishgard"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 60 && weatherVariance < 70) { weather[0] = "Fair Skies"; weather[1] = "Ishgard"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 70 && weatherVariance < 75) { weather[0] = "Clear Skies"; weather[1] = "Ishgard"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 75 && weatherVariance < 90) { weather[0] = "Clouds"; weather[1] = "Ishgard"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 90 && weatherVariance < 100) { weather[0] = "Fog"; weather[1] = "Ishgard"; weather[2] = "Coerthas"; }
            }
            else if (ActiveZone == "Coerthas Central Highlands")
            {
                if (weatherVariance >= 0 && weatherVariance < 20) { weather[0] = "Blizzards"; weather[1] = "Coerthas Central Highlands"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 20 && weatherVariance < 60) { weather[0] = "Snow"; weather[1] = "Coerthas Central Highlands"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 60 && weatherVariance < 70) { weather[0] = "Fair Skies"; weather[1] = "Coerthas Central Highlands"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 70 && weatherVariance < 75) { weather[0] = "Clear Skies"; weather[1] = "Coerthas Central Highlands"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 75 && weatherVariance < 90) { weather[0] = "Clouds"; weather[1] = "Coerthas Central Highlands"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 90 && weatherVariance < 100) { weather[0] = "Fog"; weather[1] = "Coerthas Central Highlands"; weather[2] = "Coerthas"; }
            }
            else if (ActiveZone == "Coerthas Western Highlands")
            {
                if (weatherVariance >= 0 && weatherVariance < 20) { weather[0] = "Blizzards"; weather[1] = "Coerthas Western Highlands"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 20 && weatherVariance < 60) { weather[0] = "Snow"; weather[1] = "Coerthas Western Highlands"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 60 && weatherVariance < 70) { weather[0] = "Fair Skies"; weather[1] = "Coerthas Western Highlands"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 70 && weatherVariance < 75) { weather[0] = "Clear Skies"; weather[1] = "Coerthas Western Highlands"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 75 && weatherVariance < 90) { weather[0] = "Clouds"; weather[1] = "Coerthas Western Highlands"; weather[2] = "Coerthas"; }
                else if (weatherVariance >= 90 && weatherVariance < 100) { weather[0] = "Fog"; weather[1] = "Coerthas Western Highlands"; weather[2] = "Coerthas"; }
            }
            else if (ActiveZone == "Mor Dhona")
            {
                if (weatherVariance >= 0 && weatherVariance < 15) { weather[0] = "Clouds"; weather[1] = "Mor Dhona"; weather[2] = "Mor Dhona"; }
                else if (weatherVariance >= 15 && weatherVariance < 30) { weather[0] = "Fog"; weather[1] = "Mor Dhona"; weather[2] = "Mor Dhona"; }
                else if (weatherVariance >= 30 && weatherVariance < 60) { weather[0] = "Gloom"; weather[1] = "Mor Dhona"; weather[2] = "Mor Dhona"; }
                else if (weatherVariance >= 60 && weatherVariance < 75) { weather[0] = "Clear Skies"; weather[1] = "Mor Dhona"; weather[2] = "Mor Dhona"; }
                else if (weatherVariance >= 75 && weatherVariance < 100) { weather[0] = "Fair Skies"; weather[1] = "Mor Dhona"; weather[2] = "Mor Dhona"; }
            }
            else if (ActiveZone == "Abalathia's Spine: The Sea Of Clouds")
            {
                if (weatherVariance >= 0 && weatherVariance < 30) { weather[0] = "Clear Skies"; weather[1] = "The Sea of Clouds"; weather[2] = "Abalathia's Spire"; }
                else if (weatherVariance >= 30 && weatherVariance < 60) { weather[0] = "Fair Skies"; weather[1] = "The Sea of Clouds"; weather[2] = "Abalathia's Spire"; }
                else if (weatherVariance >= 60 && weatherVariance < 70) { weather[0] = "Clouds"; weather[1] = "The Sea of Clouds"; weather[2] = "Abalathia's Spire"; }
                else if (weatherVariance >= 70 && weatherVariance < 80) { weather[0] = "Fog"; weather[1] = "The Sea of Clouds"; weather[2] = "Abalathia's Spire"; }
                else if (weatherVariance >= 80 && weatherVariance < 90) { weather[0] = "Wind"; weather[1] = "The Sea of Clouds"; weather[2] = "Abalathia's Spire"; }
                else if (weatherVariance >= 90 && weatherVariance < 100) { weather[0] = "Umbral Wind"; weather[1] = "The Sea of Clouds"; weather[2] = "Abalathia's Spire"; }
            }
            else if (ActiveZone == "Azys Lla")
            {
                if (weatherVariance >= 0 && weatherVariance < 35) { weather[0] = "Fair Skies"; weather[1] = "Azys Lla"; weather[2] = "Abalathia's Spire"; }
                else if (weatherVariance >= 35 && weatherVariance < 70) { weather[0] = "Clouds"; weather[1] = "Azys Lla"; weather[2] = "Abalathia's Spire"; }
                else if (weatherVariance >= 70 && weatherVariance < 100) { weather[0] = "Thunder"; weather[1] = "Azys Lla"; weather[2] = "Abalathia's Spire"; }
            }
            else if (ActiveZone == "The Dravanian Forelands")
            {
                if (weatherVariance >= 0 && weatherVariance < 10) { weather[0] = "Clouds"; weather[1] = "The Dravanian Forelands"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 10 && weatherVariance < 20) { weather[0] = "Fog"; weather[1] = "The Dravanian Forelands"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 20 && weatherVariance < 30) { weather[0] = "Thunger"; weather[1] = "The Dravanian Forelands"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 30 && weatherVariance < 40) { weather[0] = "Dust Storms"; weather[1] = "The Dravanian Forelands"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 40 && weatherVariance < 70) { weather[0] = "Clear Skies"; weather[1] = "The Dravanian Forelands"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 70 && weatherVariance < 100) { weather[0] = "Fair Skies"; weather[1] = "The Dravanian Forelands"; weather[2] = "Dravania"; }
            }
            else if (ActiveZone == "The Dravanian Hinterlands")
            {
                if (weatherVariance >= 0 && weatherVariance < 10) { weather[0] = "Clouds"; weather[1] = "The Dravanian Hinterlands"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 10 && weatherVariance < 20) { weather[0] = "Fog"; weather[1] = "The Dravanian Hinterlands"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 20 && weatherVariance < 30) { weather[0] = "Rain"; weather[1] = "The Dravanian Hinterlands"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 30 && weatherVariance < 40) { weather[0] = "Showers"; weather[1] = "The Dravanian Hinterlands"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 40 && weatherVariance < 70) { weather[0] = "Clear Skies"; weather[1] = "The Dravanian Hinterlands"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 70 && weatherVariance < 100) { weather[0] = "Fair Skies"; weather[1] = "The Dravanian Hinterlands"; weather[2] = "Dravania"; }
            }
            else if (ActiveZone == "The Churning Mists")
            {
                if (weatherVariance >= 0 && weatherVariance < 10) { weather[0] = "Clouds"; weather[1] = "The Churning Mists"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 10 && weatherVariance < 20) { weather[0] = "Gales"; weather[1] = "The Churning Mists"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 20 && weatherVariance < 40) { weather[0] = "Umbral Static"; weather[1] = "The Churning Mists"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 40 && weatherVariance < 70) { weather[0] = "Clear Skies"; weather[1] = "The Churning Mists"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 70 && weatherVariance < 100) { weather[0] = "Fair Skies"; weather[1] = "The Churning Mists"; weather[2] = "Dravania"; }
            }
            else if (ActiveZone == "Idyllshire")
            {
                if (weatherVariance >= 0 && weatherVariance < 10) { weather[0] = "Clouds"; weather[1] = "Idyllshire"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 10 && weatherVariance < 20) { weather[0] = "Fog"; weather[1] = "Idyllshire"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 20 && weatherVariance < 30) { weather[0] = "Rain"; weather[1] = "Idyllshire"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 30 && weatherVariance < 40) { weather[0] = "Showers"; weather[1] = "Idyllshire"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 40 && weatherVariance < 70) { weather[0] = "Clear Skies"; weather[1] = "Idyllshire"; weather[2] = "Dravania"; }
                else if (weatherVariance >= 70 && weatherVariance < 100) { weather[0] = "Fair Skies"; weather[1] = "Idyllshire"; weather[2] = "Dravania"; }
            }
            else
            {
                weather[0] = "Clear Skies"; weather[1] = "unknown"; weather[2] = "unknown";
            }

            return weather;

        }

        private void setWeather(string[] ActiveWeather)
        {
            if (ActiveWeather[1] != "unknown")
            {
                setWeatherSystem(ActiveWeather[0], ActiveWeather[1], ActiveWeather[2]);
            }
            else
            {
                updateState("static", btn_defaultCol.BackColor, btn_defaultCol.BackColor);
            }
        }

        private void setWeatherSystem(string weather, string zone, string area)
        {
            if (weather == "Clear Skies")
            {
                if (area == "La Noscea")
                {
                    updateState("static", System.Drawing.Color.DeepSkyBlue);
                }
                else if (area == "Gridania")
                {
                    updateState("static", System.Drawing.Color.MediumTurquoise);
                }
                else if (area == "Thanalan")
                {
                    updateState("static", System.Drawing.ColorTranslator.FromHtml("#f8cc35"));
                }
                else
                {
                    updateState("static", System.Drawing.ColorTranslator.FromHtml("#66d8ff"));
                }
            }
            else if (weather == "Fair Skies")
            {
                if (area == "La Noscea")
                {
                    updateState("static", System.Drawing.ColorTranslator.FromHtml("#66d8ff"));
                }
                else if (area == "Gridania")
                {
                    updateState("static", System.Drawing.Color.Turquoise);
                }
                else if (area == "Thanalan")
                {
                    updateState("static", System.Drawing.ColorTranslator.FromHtml("#f8cc35"));
                }
                else
                {
                    updateState("static", System.Drawing.Color.SkyBlue);
                }
            }
            else if (weather == "Clouds")
            {
                if (area == "La Noscea")
                {
                    updateState("static", System.Drawing.Color.AliceBlue);
                }
                else if (area == "Gridania")
                {
                    updateState("static", System.Drawing.Color.LightGreen);
                }
                else
                {
                    updateState("static", System.Drawing.Color.WhiteSmoke);
                }
            }
            else if (weather == "Fog")
            {
                if (zone == "Northern Thanalan")
                {
                    updateState("static", System.Drawing.Color.BlueViolet);
                }
                else if (area == "Gridania")
                {
                    updateState("static", System.Drawing.Color.SeaGreen);
                }
                else
                {
                    updateState("static", System.Drawing.Color.SlateGray);
                }
            }
            else if (weather == "Wind")
            {
                if (area == "La Noscea")
                {
                    updateState("static", System.Drawing.Color.PaleTurquoise);
                }
                else if (area == "Gridania")
                {
                    updateState("static", System.Drawing.Color.LightGreen);
                }
                else
                {
                    updateState("static", System.Drawing.Color.WhiteSmoke);
                }
            }
            else if (weather == "Gales")
            {
                updateState("pulse", System.Drawing.Color.MediumAquamarine, System.Drawing.Color.RoyalBlue, true, 40);
            }
            else if (weather == "Rain")
            {
                updateState("static", System.Drawing.Color.CornflowerBlue);
            }
            else if (weather == "Showers")
            {
                updateState("breath", System.Drawing.Color.CornflowerBlue);
            }
            else if (weather == "Thunder")
            {
                updateState("static", System.Drawing.Color.MidnightBlue);
            }
            else if (weather == "Thunderstorms")
            {
                updateState("breath", System.Drawing.Color.MidnightBlue, System.Drawing.Color.SteelBlue);
            }
            else if (weather == "Sandstorms")
            {
                updateState("pulse", System.Drawing.ColorTranslator.FromHtml("#f7c61c"), System.Drawing.Color.SandyBrown, true, 40);
            }
            else if (weather == "Dust Storms")
            {
                updateState("pulse", System.Drawing.ColorTranslator.FromHtml("#f7c61c"), System.Drawing.Color.SandyBrown, true, 40);
            }
            else if (weather == "Hot Spells")
            {
                updateState("static", System.Drawing.Color.OrangeRed);
            }
            else if (weather == "Heat Waves")
            {
                updateState("breath", System.Drawing.Color.Tomato, System.Drawing.Color.Orange);
            }
            else if (weather == "Snow")
            {
                updateState("static", System.Drawing.Color.Snow);
            }
            else if (weather == "Blizzards")
            {
                updateState("breath", System.Drawing.Color.GhostWhite, System.Drawing.Color.LightCyan);
            }
            else if (weather == "Auroras")
            {
                updateState("static", System.Drawing.Color.MediumSlateBlue, System.Drawing.Color.SpringGreen);
            }
            else if (weather == "Darkness")
            {
                updateState("static", System.Drawing.Color.MidnightBlue);
            }
            else if (weather == "Tension")
            {
                updateState("static", System.Drawing.Color.Indigo);
            }
            else if (weather == "Storm Clouds")
            {
                updateState("static", System.Drawing.Color.DimGray);
            }
            else if (weather == "Rough Seas")
            {
                updateState("pulse", System.Drawing.Color.DarkBlue, System.Drawing.Color.MediumBlue, true, 40);
            }
            else if (weather == "Louring")
            {
                updateState("static", System.Drawing.Color.MidnightBlue);
            }
            else if (weather == "Gloom")
            {
                updateState("breath", System.Drawing.Color.Magenta, System.Drawing.Color.Orchid);
            }
            else if (weather == "Eruptions")
            {
                updateState("breath", System.Drawing.Color.Crimson, System.Drawing.Color.OrangeRed);
            }
            else if (weather == "Irradiance")
            {
                updateState("static", System.Drawing.Color.PaleGreen);
            }
            else if (weather == "Core Radiation")
            {
                updateState("breath", System.Drawing.Color.DarkOrchid, System.Drawing.Color.MediumPurple);
            }
            else if (weather == "Shelf Clouds")
            {
                updateState("static", System.Drawing.Color.Goldenrod);
            }
            else if (weather == "Oppression")
            {
                updateState("static", System.Drawing.Color.MidnightBlue);
            }
            else if (weather == "Umbral Wind")
            {
                updateState("pulse", System.Drawing.Color.MediumSpringGreen, System.Drawing.Color.Aqua, true, 35);
            }
            else if (weather == "Umbral Static")
            {
                updateState("pulse", System.Drawing.Color.Blue, System.Drawing.Color.DodgerBlue, true, 35);
            }
            else if (weather == "Smoke")
            {
                updateState("static", System.Drawing.Color.DimGray);
            }
            else if (weather == "Royal Levin")
            {
                updateState("static", System.Drawing.Color.MediumOrchid);
            }
            else if (weather == "Hyperelectricity")
            {
                updateState("pulse", System.Drawing.Color.GreenYellow, System.Drawing.Color.YellowGreen, true, 40);
            }
        }


        //Corsair Classes
        public class KeyDataA { public int KeyID; public System.Drawing.Color KeyColor; }

        public class CorsairKeyCodes
        {
            Dictionary<int, CorsairKeyboardKeyId> sdkKeyDict;

            public CorsairKeyCodes()
            {
                sdkKeyDict = new Dictionary<int, CorsairKeyboardKeyId>();

                sdkKeyDict.Add(0, CorsairKeyboardKeyId.Escape);
                sdkKeyDict.Add(1, CorsairKeyboardKeyId.GraveAccentAndTilde);
                sdkKeyDict.Add(2, CorsairKeyboardKeyId.Tab);
                sdkKeyDict.Add(3, CorsairKeyboardKeyId.CapsLock);
                sdkKeyDict.Add(4, CorsairKeyboardKeyId.LeftShift);
                sdkKeyDict.Add(5, CorsairKeyboardKeyId.LeftCtrl);
                sdkKeyDict.Add(6, CorsairKeyboardKeyId.F12);
                sdkKeyDict.Add(7, CorsairKeyboardKeyId.EqualsAndPlus);
                sdkKeyDict.Add(8, CorsairKeyboardKeyId.WinLock);
                sdkKeyDict.Add(9, CorsairKeyboardKeyId.Keypad7);
                sdkKeyDict.Add(10, CorsairKeyboardKeyId.G1);
                sdkKeyDict.Add(11, CorsairKeyboardKeyId.MR);
                sdkKeyDict.Add(12, CorsairKeyboardKeyId.F1);
                sdkKeyDict.Add(13, CorsairKeyboardKeyId.D1);
                sdkKeyDict.Add(14, CorsairKeyboardKeyId.Q);
                sdkKeyDict.Add(15, CorsairKeyboardKeyId.A);
                sdkKeyDict.Add(16, CorsairKeyboardKeyId.NonUsBackslash);
                sdkKeyDict.Add(17, CorsairKeyboardKeyId.LeftGui);
                sdkKeyDict.Add(18, CorsairKeyboardKeyId.PrintScreen);
                sdkKeyDict.Add(19, CorsairKeyboardKeyId.Invalid);
                sdkKeyDict.Add(20, CorsairKeyboardKeyId.Mute);
                sdkKeyDict.Add(21, CorsairKeyboardKeyId.Keypad8);
                sdkKeyDict.Add(22, CorsairKeyboardKeyId.G2);
                sdkKeyDict.Add(23, CorsairKeyboardKeyId.M1);
                sdkKeyDict.Add(24, CorsairKeyboardKeyId.F2);
                sdkKeyDict.Add(25, CorsairKeyboardKeyId.D2);
                sdkKeyDict.Add(26, CorsairKeyboardKeyId.W);
                sdkKeyDict.Add(27, CorsairKeyboardKeyId.S);
                sdkKeyDict.Add(28, CorsairKeyboardKeyId.Z);
                sdkKeyDict.Add(29, CorsairKeyboardKeyId.LeftAlt);
                sdkKeyDict.Add(30, CorsairKeyboardKeyId.ScrollLock);
                sdkKeyDict.Add(31, CorsairKeyboardKeyId.Backspace);
                sdkKeyDict.Add(32, CorsairKeyboardKeyId.Stop);
                sdkKeyDict.Add(33, CorsairKeyboardKeyId.Keypad9);
                sdkKeyDict.Add(34, CorsairKeyboardKeyId.G3);
                sdkKeyDict.Add(35, CorsairKeyboardKeyId.M2);
                sdkKeyDict.Add(36, CorsairKeyboardKeyId.F3);
                sdkKeyDict.Add(37, CorsairKeyboardKeyId.D3);
                sdkKeyDict.Add(38, CorsairKeyboardKeyId.E);
                sdkKeyDict.Add(39, CorsairKeyboardKeyId.D);
                sdkKeyDict.Add(40, CorsairKeyboardKeyId.X);
                sdkKeyDict.Add(41, CorsairKeyboardKeyId.Invalid);
                sdkKeyDict.Add(42, CorsairKeyboardKeyId.PauseBreak);
                sdkKeyDict.Add(43, CorsairKeyboardKeyId.Delete);
                sdkKeyDict.Add(44, CorsairKeyboardKeyId.ScanPreviousTrack);
                sdkKeyDict.Add(45, CorsairKeyboardKeyId.Invalid);
                sdkKeyDict.Add(46, CorsairKeyboardKeyId.G4);
                sdkKeyDict.Add(47, CorsairKeyboardKeyId.M3);
                sdkKeyDict.Add(48, CorsairKeyboardKeyId.F4);
                sdkKeyDict.Add(49, CorsairKeyboardKeyId.D4);
                sdkKeyDict.Add(50, CorsairKeyboardKeyId.R);
                sdkKeyDict.Add(51, CorsairKeyboardKeyId.F);
                sdkKeyDict.Add(52, CorsairKeyboardKeyId.C);
                sdkKeyDict.Add(53, CorsairKeyboardKeyId.Space);
                sdkKeyDict.Add(54, CorsairKeyboardKeyId.Insert);
                sdkKeyDict.Add(55, CorsairKeyboardKeyId.End);
                sdkKeyDict.Add(56, CorsairKeyboardKeyId.PlayPause);
                sdkKeyDict.Add(57, CorsairKeyboardKeyId.Keypad4);
                sdkKeyDict.Add(58, CorsairKeyboardKeyId.G5);
                sdkKeyDict.Add(59, CorsairKeyboardKeyId.G11);
                sdkKeyDict.Add(60, CorsairKeyboardKeyId.F5);
                sdkKeyDict.Add(61, CorsairKeyboardKeyId.D5);
                sdkKeyDict.Add(62, CorsairKeyboardKeyId.T);
                sdkKeyDict.Add(63, CorsairKeyboardKeyId.G);
                sdkKeyDict.Add(64, CorsairKeyboardKeyId.V);
                sdkKeyDict.Add(65, CorsairKeyboardKeyId.Invalid);
                sdkKeyDict.Add(66, CorsairKeyboardKeyId.Home);
                sdkKeyDict.Add(67, CorsairKeyboardKeyId.PageDown);
                sdkKeyDict.Add(68, CorsairKeyboardKeyId.ScanNextTrack);
                sdkKeyDict.Add(69, CorsairKeyboardKeyId.Keypad5);
                sdkKeyDict.Add(70, CorsairKeyboardKeyId.G6);
                sdkKeyDict.Add(71, CorsairKeyboardKeyId.G12);
                sdkKeyDict.Add(72, CorsairKeyboardKeyId.F6);
                sdkKeyDict.Add(73, CorsairKeyboardKeyId.D6);
                sdkKeyDict.Add(74, CorsairKeyboardKeyId.Y);
                sdkKeyDict.Add(75, CorsairKeyboardKeyId.H);
                sdkKeyDict.Add(76, CorsairKeyboardKeyId.B);
                sdkKeyDict.Add(77, CorsairKeyboardKeyId.Invalid);
                sdkKeyDict.Add(78, CorsairKeyboardKeyId.PageUp);
                sdkKeyDict.Add(79, CorsairKeyboardKeyId.RightShift);
                sdkKeyDict.Add(80, CorsairKeyboardKeyId.NumLock);
                sdkKeyDict.Add(81, CorsairKeyboardKeyId.Keypad6);
                sdkKeyDict.Add(82, CorsairKeyboardKeyId.G7);
                sdkKeyDict.Add(83, CorsairKeyboardKeyId.G13);
                sdkKeyDict.Add(84, CorsairKeyboardKeyId.F7);
                sdkKeyDict.Add(85, CorsairKeyboardKeyId.D7);
                sdkKeyDict.Add(86, CorsairKeyboardKeyId.U);
                sdkKeyDict.Add(87, CorsairKeyboardKeyId.J);
                sdkKeyDict.Add(88, CorsairKeyboardKeyId.N);
                sdkKeyDict.Add(89, CorsairKeyboardKeyId.RightAlt);
                sdkKeyDict.Add(90, CorsairKeyboardKeyId.BracketRight);
                sdkKeyDict.Add(91, CorsairKeyboardKeyId.RightCtrl);
                sdkKeyDict.Add(92, CorsairKeyboardKeyId.KeypadSlash);
                sdkKeyDict.Add(93, CorsairKeyboardKeyId.Keypad1);
                sdkKeyDict.Add(94, CorsairKeyboardKeyId.G8);
                sdkKeyDict.Add(95, CorsairKeyboardKeyId.G14);
                sdkKeyDict.Add(96, CorsairKeyboardKeyId.F8);
                sdkKeyDict.Add(97, CorsairKeyboardKeyId.D8);
                sdkKeyDict.Add(98, CorsairKeyboardKeyId.I);
                sdkKeyDict.Add(99, CorsairKeyboardKeyId.K);
                sdkKeyDict.Add(100, CorsairKeyboardKeyId.M);
                sdkKeyDict.Add(101, CorsairKeyboardKeyId.RightGui);
                sdkKeyDict.Add(102, CorsairKeyboardKeyId.Backslash);
                sdkKeyDict.Add(103, CorsairKeyboardKeyId.UpArrow);
                sdkKeyDict.Add(104, CorsairKeyboardKeyId.KeypadAsterisk);
                sdkKeyDict.Add(105, CorsairKeyboardKeyId.Keypad2);
                sdkKeyDict.Add(106, CorsairKeyboardKeyId.G9);
                sdkKeyDict.Add(107, CorsairKeyboardKeyId.G15);
                sdkKeyDict.Add(108, CorsairKeyboardKeyId.F9);
                sdkKeyDict.Add(109, CorsairKeyboardKeyId.D9);
                sdkKeyDict.Add(110, CorsairKeyboardKeyId.O);
                sdkKeyDict.Add(111, CorsairKeyboardKeyId.L);
                sdkKeyDict.Add(112, CorsairKeyboardKeyId.CommaAndLessThan);
                sdkKeyDict.Add(113, CorsairKeyboardKeyId.Application);
                sdkKeyDict.Add(114, CorsairKeyboardKeyId.Invalid);
                sdkKeyDict.Add(115, CorsairKeyboardKeyId.LeftArrow);
                sdkKeyDict.Add(116, CorsairKeyboardKeyId.KeypadMinus);
                sdkKeyDict.Add(117, CorsairKeyboardKeyId.Keypad3);
                sdkKeyDict.Add(118, CorsairKeyboardKeyId.G10);
                sdkKeyDict.Add(119, CorsairKeyboardKeyId.G16);
                sdkKeyDict.Add(120, CorsairKeyboardKeyId.F10);
                sdkKeyDict.Add(121, CorsairKeyboardKeyId.D0);
                sdkKeyDict.Add(122, CorsairKeyboardKeyId.P);
                sdkKeyDict.Add(123, CorsairKeyboardKeyId.SemicolonAndColon);
                sdkKeyDict.Add(124, CorsairKeyboardKeyId.PeriodAndBiggerThan);
                sdkKeyDict.Add(125, CorsairKeyboardKeyId.CLK_Logo);
                sdkKeyDict.Add(126, CorsairKeyboardKeyId.Enter);
                sdkKeyDict.Add(127, CorsairKeyboardKeyId.DownArrow);
                sdkKeyDict.Add(128, CorsairKeyboardKeyId.KeypadPlus);
                sdkKeyDict.Add(129, CorsairKeyboardKeyId.Keypad0);
                sdkKeyDict.Add(130, CorsairKeyboardKeyId.Invalid);
                sdkKeyDict.Add(131, CorsairKeyboardKeyId.G17);
                sdkKeyDict.Add(132, CorsairKeyboardKeyId.F11);
                sdkKeyDict.Add(133, CorsairKeyboardKeyId.MinusAndUnderscore);
                sdkKeyDict.Add(134, CorsairKeyboardKeyId.BracketLeft);
                sdkKeyDict.Add(135, CorsairKeyboardKeyId.ApostropheAndDoubleQuote);
                sdkKeyDict.Add(136, CorsairKeyboardKeyId.SlashAndQuestionMark);
                sdkKeyDict.Add(137, CorsairKeyboardKeyId.Brightness);
                sdkKeyDict.Add(138, CorsairKeyboardKeyId.Invalid);
                sdkKeyDict.Add(139, CorsairKeyboardKeyId.RightArrow);
                sdkKeyDict.Add(140, CorsairKeyboardKeyId.KeypadEnter);
                sdkKeyDict.Add(141, CorsairKeyboardKeyId.KeypadPeriodAndDelete);
                sdkKeyDict.Add(142, CorsairKeyboardKeyId.Invalid);
                sdkKeyDict.Add(143, CorsairKeyboardKeyId.G18);
                sdkKeyDict.Add(144, CorsairKeyboardKeyId.Invalid);

            }

            public CorsairKeyboardKeyId GetKeyCodeFromDict(int key)
            { return (sdkKeyDict[key]); }
        }

        public void UpdateKeyboard(System.Drawing.Color col)
        {
            CorsairKeyboardKeyId ckey;
            CorsairKeyCodes keySDK = new CorsairKeyCodes();
            CorsairKeyboard keyboard = CueSDK.KeyboardSDK;
            int t;
            
            for (t = 0; t < 144; t++)
            {
                ckey = keySDK.GetKeyCodeFromDict(t);
                if (ckey != CorsairKeyboardKeyId.Invalid)
                {
                    try {
                        keyboard[ckey].Led.Color = col;
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
            keyboard.Update();
        }
    }
 }
 

//Logitech Classes
namespace LedCSharp
{

    public enum keyboardNames
    {
        ESC = 0x01,
        F1 = 0x3b,
        F2 = 0x3c,
        F3 = 0x3d,
        F4 = 0x3e,
        F5 = 0x3f,
        F6 = 0x40,
        F7 = 0x41,
        F8 = 0x42,
        F9 = 0x43,
        F10 = 0x44,
        F11 = 0x57,
        F12 = 0x58,
        PRINT_SCREEN = 0x137,
        SCROLL_LOCK = 0x46,
        PAUSE_BREAK = 0x45,
        TILDE = 0x29,
        ONE = 0x02,
        TWO = 0x03,
        THREE = 0x04,
        FOUR = 0x05,
        FIVE = 0x06,
        SIX = 0x07,
        SEVEN = 0x08,
        EIGHT = 0x09,
        NINE = 0x0A,
        ZERO = 0x0B,
        MINUS = 0x0C,
        EQUALS = 0x0D,
        BACKSPACE = 0x0E,
        INSERT = 0x152,
        HOME = 0x147,
        PAGE_UP = 0x149,
        NUM_LOCK = 0x145,
        NUM_SLASH = 0x135,
        NUM_ASTERISK = 0x37,
        NUM_MINUS = 0x4A,
        TAB = 0x0F,
        Q = 0x10,
        W = 0x11,
        E = 0x12,
        R = 0x13,
        T = 0x14,
        Y = 0x15,
        U = 0x16,
        I = 0x17,
        O = 0x18,
        P = 0x19,
        OPEN_BRACKET = 0x1A,
        CLOSE_BRACKET = 0x1B,
        BACKSLASH = 0x2B,
        KEYBOARD_DELETE = 0x153,
        END = 0x14F,
        PAGE_DOWN = 0x151,
        NUM_SEVEN = 0x47,
        NUM_EIGHT = 0x48,
        NUM_NINE = 0x49,
        NUM_PLUS = 0x4E,
        CAPS_LOCK = 0x3A,
        A = 0x1E,
        S = 0x1F,
        D = 0x20,
        F = 0x21,
        G = 0x22,
        H = 0x23,
        J = 0x24,
        K = 0x25,
        L = 0x26,
        SEMICOLON = 0x27,
        APOSTROPHE = 0x28,
        ENTER = 0x1C,
        NUM_FOUR = 0x4B,
        NUM_FIVE = 0x4C,
        NUM_SIX = 0x4D,
        LEFT_SHIFT = 0x2A,
        Z = 0x2C,
        X = 0x2D,
        C = 0x2E,
        V = 0x2F,
        B = 0x30,
        N = 0x31,
        M = 0x32,
        COMMA = 0x33,
        PERIOD = 0x34,
        FORWARD_SLASH = 0x35,
        RIGHT_SHIFT = 0x36,
        ARROW_UP = 0x148,
        NUM_ONE = 0x4F,
        NUM_TWO = 0x50,
        NUM_THREE = 0x51,
        NUM_ENTER = 0x11C,
        LEFT_CONTROL = 0x1D,
        LEFT_WINDOWS = 0x15B,
        LEFT_ALT = 0x38,
        SPACE = 0x39,
        RIGHT_ALT = 0x138,
        RIGHT_WINDOWS = 0x15C,
        APPLICATION_SELECT = 0x15D,
        RIGHT_CONTROL = 0x11D,
        ARROW_LEFT = 0x14B,
        ARROW_DOWN = 0x150,
        ARROW_RIGHT = 0x14D,
        NUM_ZERO = 0x52,
        NUM_PERIOD = 0x53,

    };

    public class LogitechGSDK
    {
        //LED SDK
        private const int LOGI_DEVICETYPE_MONOCHROME_ORD = 0;
        private const int LOGI_DEVICETYPE_RGB_ORD = 1;
        private const int LOGI_DEVICETYPE_PERKEY_RGB_ORD = 2;

        public const int LOGI_DEVICETYPE_MONOCHROME = (1 << LOGI_DEVICETYPE_MONOCHROME_ORD);
        public const int LOGI_DEVICETYPE_RGB = (1 << LOGI_DEVICETYPE_RGB_ORD);
        public const int LOGI_DEVICETYPE_PERKEY_RGB = (1 << LOGI_DEVICETYPE_PERKEY_RGB_ORD);
        public const int LOGI_LED_BITMAP_WIDTH = 21;
        public const int LOGI_LED_BITMAP_HEIGHT = 6;
        public const int LOGI_LED_BITMAP_BYTES_PER_KEY = 4;

        public const int LOGI_LED_BITMAP_SIZE = LOGI_LED_BITMAP_WIDTH * LOGI_LED_BITMAP_HEIGHT * LOGI_LED_BITMAP_BYTES_PER_KEY;
        public const int LOGI_LED_DURATION_INFINITE = 0;
        
        #region Libary Management

        static LogitechGSDK()
        {
            string externalPluginEnv = Chromatics.Chromatics.pluginEnviroment;
            LoadLibrary(externalPluginEnv + "\\lib\\LogitechLedEnginesWrapper ");
            //Debug.WriteLine(externalPluginEnv);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);
        #endregion

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedInit();

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetTargetDevice(int targetDevice);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedGetSdkVersion(ref int majorNum, ref int minorNum, ref int buildNum);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSaveCurrentLighting();

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetLighting(int redPercentage, int greenPercentage, int bluePercentage);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedRestoreLighting();

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedFlashLighting(int redPercentage, int greenPercentage, int bluePercentage, int milliSecondsDuration, int milliSecondsInterval);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedPulseLighting(int redPercentage, int greenPercentage, int bluePercentage, int milliSecondsDuration, int milliSecondsInterval);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedStopEffects();

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetLightingFromBitmap(byte[] bitmap);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetLightingForKeyWithScanCode(int keyCode, int redPercentage, int greenPercentage, int bluePercentage);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetLightingForKeyWithHidCode(int keyCode, int redPercentage, int greenPercentage, int bluePercentage);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetLightingForKeyWithQuartzCode(int keyCode, int redPercentage, int greenPercentage, int bluePercentage);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetLightingForKeyWithKeyName(keyboardNames keyCode, int redPercentage, int greenPercentage, int bluePercentage);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSaveLightingForKey(keyboardNames keyName);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedRestoreLightingForKey(keyboardNames keyName);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedFlashSingleKey(keyboardNames keyName, int redPercentage, int greenPercentage, int bluePercentage, int msDuration, int msInterval);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedPulseSingleKey(keyboardNames keyName, int startRedPercentage, int startGreenPercentage, int startBluePercentage, int finishRedPercentage, int finishGreenPercentage, int finishBluePercentage, int msDuration, bool isInfinite);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedStopEffectsOnKey(keyboardNames keyName);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern void LogiLedShutdown();
    }
}