using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharlayan;
using Sharlayan.Enums;
using Sharlayan.Models;
using Chromatics.Helpers;
using Chromatics.Layers;
using Chromatics.Interfaces;
using Chromatics.Enums;
using System.Threading;
using Chromatics.Extensions.RGB.NET;
using RGB.NET.Core;
using RGB.NET.Presets.Decorators;
using RGB.NET.Presets.Textures.Gradients;
using RGB.NET.Presets.Textures;
using Chromatics.Extensions.RGB.NET.Decorators;
using static MetroFramework.Drawing.MetroPaint;
using System.Security.Policy;
using Sharlayan.Utilities;

namespace Chromatics.Core
{
    public static class GameController
    {
        private static MemoryHandler _memoryHandler;
        private static CustomComparers.LayerComparer comparer = new();
        private static CancellationTokenSource _GameConnectionCancellationTokenSource = new CancellationTokenSource();
        private static CancellationTokenSource _GameLoopCancellationTokenSource = new CancellationTokenSource();
        private static readonly int _loopInterval = 200;
        private static readonly int _connectionInterval = 10000;
        private static int _connectionAttempts = 0;
        private static bool gameConnected;
        private static bool gameSetup;
        private static bool memoryEfficientLoop;
        private static bool _isInGame;
        private static bool _onTitle;
        private static bool wasPreviewed;
        public static void Setup()
        {
            if (gameSetup) return;

            comparer = new CustomComparers.LayerComparer();
            
            if (!gameConnected)
            {
                RGBController.StopEffects();
                RGBController.RunStartupEffects();
                Task.Run(() => GameConnectionLoop(_GameConnectionCancellationTokenSource.Token));
            }

            gameSetup = true;
            RGBController.PreviewTriggered += OnPreviewTriggered;
        }

        public static void Stop(bool reconnect = false)
        {
            RGBController.StopEffects();
            Logger.WriteConsole(LoggerTypes.FFXIV, @"Stopping FFXIV Connection..");
            StopGameLoop(reconnect);
            _GameConnectionCancellationTokenSource.Cancel();
        }

        public static bool IsGameConnected()
        {
            return gameConnected;
        }

        public static MemoryHandler GetGameData()
        {
            if (gameSetup && gameConnected)
                return _memoryHandler;

            return null;
        }

        public static Process GetGameProcess()
        {
            if (gameSetup && gameConnected)
                return _memoryHandler.Configuration.ProcessModel.Process;

            return null;
        }

        private static void StartGameLoop()
        {
            _GameLoopCancellationTokenSource.Dispose();
            _GameLoopCancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => GameLoop(_GameLoopCancellationTokenSource.Token));
        }

        private static void StopGameLoop(bool reconnect = false)
        {
            _GameLoopCancellationTokenSource.Cancel();

            if (_memoryHandler != null)
            {
                Debug.WriteLine(@"Disposed Memory Handler object.");
                _memoryHandler.Dispose();
            }

            if (reconnect)
            {
                _GameConnectionCancellationTokenSource.Dispose();
                _GameConnectionCancellationTokenSource = new CancellationTokenSource();
                RGBController.StopEffects();
                RGBController.RunStartupEffects();
                Task.Run(() => GameConnectionLoop(_GameConnectionCancellationTokenSource.Token));
            }
        }

        private static async Task GameLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsGameRunning())
                {
                    GameProcessLayers();
                }
                else
                {
                    gameConnected = false;
                    _isInGame = false;
                    _onTitle = false;

                    Logger.WriteConsole(LoggerTypes.FFXIV, @"Lost connection to FFXIV. Will attempt to reconnect.");

                    StopGameLoop(true);
                }
                                
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Wait for the interval before continuing
                var delay = _loopInterval;
                
                if (memoryEfficientLoop)
                {
                    var currentCpuUsage = SystemMonitorHelper.GetCurrentCpuUsage();
                    var _maxCpuUsage = SystemMonitorHelper.GetMaxCpuUsage();

                    if (currentCpuUsage > _maxCpuUsage)
                    {
                        delay += (int)(currentCpuUsage - _maxCpuUsage) * 10;
                        Debug.WriteLine(@"Delay: " + delay);
                    }
                }
                
                await Task.Delay(delay, cancellationToken);
            }
        }

        private static async Task GameConnectionLoop(CancellationToken cancellationToken)
        {   
            while (!cancellationToken.IsCancellationRequested)
            {
                if (gameConnected)
                {
                    //_GameConnectionCancellationTokenSource.Cancel();
                    //RGBController.StopEffects(true);
                    //StartGameLoop();
                    break;
                }
                else
                {
                    ConnectFFXIVClient();
                }
                
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Wait for the interval before continuing
                var delay = _connectionInterval;

                await Task.Delay(delay, cancellationToken);
            }
        }

        private static bool IsGameRunning()
        {
            var processes = Process.GetProcessesByName("ffxiv_dx11");
            if (processes.Length > 0)
            {
                return true;
            }

            return false;
        }

        private static void ConnectFFXIVClient()
        {
            try
            {
                if (_connectionAttempts < 1)
                {
                    Logger.WriteConsole(LoggerTypes.FFXIV, @"Attempting to attach to FFXIV..");
                }

                if (_connectionAttempts == 5)
                {
                    Logger.WriteConsole(LoggerTypes.FFXIV, @"Cannot find FFXIV process. Is the game running?");
                    Logger.WriteConsole(LoggerTypes.FFXIV, @"Attempting to attach to FFXIV..");
                }
            
                _connectionAttempts++;
                Debug.WriteLine(@"Attempting to attach to FFXIV. Attempt: " + _connectionAttempts);

                var processes = Process.GetProcessesByName("ffxiv_dx11");
                if (processes.Length > 0)
                {
                    // supported: Global, Chinese, Korean
                    var gameRegion = GameRegion.Global;
                    var gameLanguage = GameLanguage.English;

	                // patchVersion of game, or latest
	                var patchVersion = "latest";
                    var process = processes[0];
                    var processModel = new ProcessModel {
                        Process = process
                    };
                
                    SharlayanConfiguration configuration = new SharlayanConfiguration {
                        ProcessModel = processModel,
                        GameLanguage = gameLanguage,
                        GameRegion = gameRegion,
                        PatchVersion = patchVersion,
                        UseLocalCache = AppSettings.GetSettings().localcache
                    };             

                    Debug.WriteLine($"Using Local Cache: {AppSettings.GetSettings().localcache}");

                    _memoryHandler = SharlayanMemoryManager.Instance.AddHandler(configuration);
                    
                    gameConnected = true;
                
                }

                if (gameConnected)
                {
                    Logger.WriteConsole(LoggerTypes.FFXIV, @"Attached to FFXIV.");
                    _connectionAttempts = 0;

                    _GameConnectionCancellationTokenSource.Cancel();
                    RGBController.StopEffects();
                    RGBController.ResetLayerGroups();
                    StartGameLoop();

                    #if DEBUG
                        Debug.WriteLine(@"Scanning memory..");
                        Thread.Sleep(1000);
                        foreach (var location in _memoryHandler.Scanner.Locations)
                        {
                            Debug.WriteLine($"Found {location.Key}. Location: {location.Value.GetAddress().ToInt64():X}");
                        }
                    #endif
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"Exception: " + ex.Message);

                if (ex.Message == "Access is denied.")
                {
                    Logger.WriteConsole(LoggerTypes.Error, @"Unable to attach to FFXIV process. Are you running Chromatics as an administrator?");
                    Logger.WriteConsole(LoggerTypes.Error, @"Please restart Chromatics and try again.");
                }
            }
            
        }

        private static void GameProcessLayers()
        {
            if (!gameConnected) return;

            //Check if game has logged in
            
            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors() && _memoryHandler.Reader.CanGetChatLog())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                var chatLogCount = _memoryHandler.Reader.GetChatLog().ChatLogItems.Count;

                var runningEffects = RGBController.GetRunningEffects();

                if (getCurrentPlayer.Entity == null && chatLogCount <= 0)
                {
                    //Game is still on Main Menu or Character Screen
                    if (!_onTitle || wasPreviewed)
                    {
                        RGBController.StopEffects();
                        RGBController.ResetLayerGroups();

                        if (RGBController.GetEffectsSettings().effect_titlescreen)
                        {
                            var surface = RGBController.GetLiveSurfaces();
                            var devices = surface.GetDevices(RGBDeviceType.All);
                            var _colorPalette = RGBController.GetActivePalette();
                            var baseColor = ColorHelper.ColorToRGBColor(_colorPalette.MenuBase.Color);
                            var highlightColors = new Color[] {
                                ColorHelper.ColorToRGBColor(_colorPalette.MenuHighlight1.Color),
                                ColorHelper.ColorToRGBColor(_colorPalette.MenuHighlight2.Color),
                                ColorHelper.ColorToRGBColor(_colorPalette.MenuHighlight3.Color)
                            };

                            foreach (var device in devices)
                            {
                                var ledgroup = new PublicListLedGroup(surface, device);
                        
                                var starfield = new StarfieldDecorator(ledgroup, (ledgroup.PublicGroupLeds.Count / 4), 10, 500, highlightColors, surface, false, baseColor);
                                ledgroup.ZIndex = 1000;
                            
                                foreach (var led in device)
                                {
                                    ledgroup.AddLed(led);
                                }

                                ledgroup.Brush = new SolidColorBrush(baseColor);
                                ledgroup.AddDecorator(starfield);                    

                                runningEffects.Add(ledgroup);
                                                        
                            }
                                                        

                        }
                        Debug.WriteLine(@"User on title or character screen");
                        _onTitle = true;
                        wasPreviewed = false;
                    }
                    
                    _isInGame = false;
                    
                }
                else
                {
                    //Character has logged in
                    _isInGame = true;

                    if (_onTitle)
                    {
                        Debug.WriteLine(@"User logging in to FFXIV..");
                        RGBController.StopEffects();
                        RGBController.ResetLayerGroups();
                        _onTitle = false;
                    }
                    
                }
                
            }
            
            if (!_isInGame) return;

            var _layers = MappingLayers.GetLayers();

            foreach (IMappingLayer layer in _layers.Values.OrderBy(x => x.zindex, comparer))
            {
                // Perform processing on the layer
                //if (!layer.Enabled) continue;
                                
                switch (layer.rootLayerType)
                {
                    case LayerType.BaseLayer:

                        var baseLayerProcessors = BaseLayerProcessorFactory.GetProcessors();
                        baseLayerProcessors[(BaseLayerType)layer.layerTypeindex].Process(layer);
                        break;

                    case LayerType.DynamicLayer:

                        var dynamicLayerProcessors = DynamicLayerProcessorFactory.GetProcessors();
                        dynamicLayerProcessors[(DynamicLayerType)layer.layerTypeindex].Process(layer);
                        break;

                    case LayerType.EffectLayer:
                        //Redo this logic by calling each type of layer manually or all of them from a loop

                        /*
                        var effectLayerProcessors = EffectLayerProcessorFactory.GetProcessors();
                        effectLayerProcessors[(EffectLayerType)layer.layerTypeindex].Process(layer);
                        */
                        break;
                       
                }


            }
            
        }

        private static void OnPreviewTriggered()
        {
            if (!gameConnected) return;
            
            if (!wasPreviewed)
                wasPreviewed = true;
        }
    }
}
