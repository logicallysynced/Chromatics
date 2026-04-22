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
using RGB.NET.Core;
using Chromatics.Extensions.RGB.NET.Decorators;
using Sharlayan.Core.Enums;

namespace Chromatics.Core
{
    public delegate void JobChanged();

    public static class GameController
    {
        private static LayerProcessorFactory _layerProcessorFactory;
        private static MemoryHandler _memoryHandler;
        public static event JobChanged jobChanged;
        private static LayerComparer comparer = new();
        private static CancellationTokenSource _GameConnectionCancellationTokenSource = new CancellationTokenSource();
        private static CancellationTokenSource _GameLoopCancellationTokenSource = new CancellationTokenSource();
        private static CancellationTokenSource _masterCancellationToken = new CancellationTokenSource();
        private static Actor.Job _currentJob;
        private static SharlayanConfiguration _configuration;
        private static readonly int _loopInterval = 200;
        // Poll quickly for the first minute, then back off — if the user hasn't
        // launched FFXIV yet, they're unlikely to launch in the next few seconds,
        // and scanning the full OS process list 6 times a minute forever is waste.
        private static readonly int _connectionInterval = 10000;       // first minute
        private static readonly int _connectionIntervalSlow = 30000;   // after first minute
        private static readonly int _fastAttemptThreshold = 6;         // ~60s at 10s cadence
        private static int _connectionAttempts = 0;
        // `-1` is the sentinel meaning "no active FFXIV process". Default-int `0`
        // would slip past the `!= -1` guard in StopGameLoop and call into Sharlayan
        // before a connection was ever established.
        private static int activeProcessId = -1;
        private static bool gameConnected;
        private static bool gameSetup;
        private static bool memoryEfficientLoop;
        private static bool _isInGame;
        private static bool _onTitle;
        private static bool wasPreviewed;
        // Set to true by Exit() before any teardown, so that concurrent loops on
        // the thread pool bail out before touching disposed CancellationTokenSources.
        private static volatile bool _isShuttingDown;
        // Serializes all CTS lifecycle operations (Cancel/Dispose/reassign) and
        // memory-handler teardown. Without this, GameLoop (thread pool) and Exit()
        // (UI thread) can race on StopGameLoop and hit ObjectDisposedException.
        private static readonly System.Threading.Lock _shutdownLock = new();
        public static System.Action OnGameExited { get; set; }
        public static void Setup()
        {
            if (gameSetup) return;

            _layerProcessorFactory = LayerProcessorFactory.Instance;
            comparer = new LayerComparer();


            if (!gameConnected)
            {
                RGBController.StopEffects();
                RGBController.RunStartupEffects();
                Task.Run(() => GameConnectionLoop(_GameConnectionCancellationTokenSource.Token))
                    .ContinueWith(
                        t => Logger.WriteConsole(LoggerTypes.Error, $"GameConnectionLoop faulted: {t.Exception?.GetBaseException()?.Message}"),
                        TaskContinuationOptions.OnlyOnFaulted);
            }

            gameSetup = true;
        }

        public static void Exit()
        {
            lock (_shutdownLock)
            {
                if (_isShuttingDown) return;
                _isShuttingDown = true;

                // Cancel tokens but do NOT dispose: in-flight background tasks may
                // still observe these CancellationTokenSources briefly after Exit()
                // returns, and Dispose() would make that a hard crash. The CTSes
                // hold no unmanaged resources; the process is exiting, so the GC
                // reclaims them shortly.
                SafeCancel(_GameConnectionCancellationTokenSource);
                SafeCancel(_GameLoopCancellationTokenSource);
                SafeCancel(_masterCancellationToken);

                _memoryHandler?.Dispose();
                _memoryHandler = null;

                if (activeProcessId != -1)
                {
                    SharlayanMemoryManager.Instance.RemoveHandler(activeProcessId);
                    activeProcessId = -1;
                }

                _configuration?.ProcessModel?.Process?.Dispose();
                _configuration = null;

                _layerProcessorFactory?.DisposeAll();
            }
        }

        public static void Stop(bool reconnect = false)
        {
            RGBController.StopEffects();
            Logger.WriteConsole(LoggerTypes.FFXIV, @"Stopping FFXIV Connection..");

            if (jobChanged != null)
            {
                foreach (Delegate d in jobChanged.GetInvocationList())
                {
                    jobChanged -= (JobChanged)d;
                }
            }

            StopGameLoop(reconnect);
            SafeCancel(_GameConnectionCancellationTokenSource);
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
                return _memoryHandler?.Configuration?.ProcessModel?.Process;

            return null;
        }

        public static Actor.Job GetCurrectJob()
        {
            if (gameSetup && gameConnected)
            {
                var handler = _memoryHandler;
                if (handler?.Reader != null && handler.Reader.CanGetActors())
                {
                    var getCurrentPlayer = handler.Reader.GetCurrentPlayer();
                    if (getCurrentPlayer.Entity != null)
                    {
                        return getCurrentPlayer.Entity.Job;
                    }
                }
            }

            return Actor.Job.Unknown;
        }

        private static void StartGameLoop()
        {
            lock (_shutdownLock)
            {
                if (_isShuttingDown) return;

                try { _GameLoopCancellationTokenSource.Dispose(); } catch (ObjectDisposedException) { }
                _GameLoopCancellationTokenSource = new CancellationTokenSource();
                var loopToken = _GameLoopCancellationTokenSource.Token;
                Task.Run(() => GameLoop(loopToken))
                    .ContinueWith(
                        t => Logger.WriteConsole(LoggerTypes.Error, $"GameLoop faulted: {t.Exception?.GetBaseException()?.Message}"),
                        TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private static void StopGameLoop(bool reconnect = false)
        {
            lock (_shutdownLock)
            {
                if (_isShuttingDown && !reconnect)
                {
                    // Exit() owns the final teardown; don't race it from here.
                    return;
                }

                SafeCancel(_GameLoopCancellationTokenSource);

                _memoryHandler?.Dispose();
                _memoryHandler = null;

                if (activeProcessId != -1)
                {
                    SharlayanMemoryManager.Instance.RemoveHandler(activeProcessId);
                    activeProcessId = -1;
                }

                // `_configuration` is only assigned once we've successfully connected to
                // FFXIV, so this can run with it still null (game was never running, or
                // a prior StopGameLoop nulled it out already).
                _configuration?.ProcessModel?.Process?.Dispose();
                _configuration = null;

                // Same rationale as `_configuration` — Setup() may not have run if the
                // user exits from the first-run wizard or during early startup errors.
                _layerProcessorFactory?.DisposeAll();

                if (reconnect && !_isShuttingDown)
                {
                    try { _GameConnectionCancellationTokenSource.Dispose(); } catch (ObjectDisposedException) { }
                    _GameConnectionCancellationTokenSource = new CancellationTokenSource();
                    var reconnectToken = _GameConnectionCancellationTokenSource.Token;

                    RGBController.StopEffects();
                    RGBController.RunStartupEffects();
                    Task.Run(() => GameConnectionLoop(reconnectToken))
                        .ContinueWith(
                            t => Logger.WriteConsole(LoggerTypes.Error, $"GameConnectionLoop (reconnect) faulted: {t.Exception?.GetBaseException()?.Message}"),
                            TaskContinuationOptions.OnlyOnFaulted);
                }
            }
        }

        private static void SafeCancel(CancellationTokenSource cts)
        {
            if (cts == null) return;
            try { cts.Cancel(); }
            catch (ObjectDisposedException) { }
        }

        private static async Task GameLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !_isShuttingDown)
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

                    if (AppSettings.GetSettings().closeWithGame)
                    {
                        Logger.WriteConsole(LoggerTypes.FFXIV, "Closing Chromatics (Close with Game is enabled).");
                        OnGameExited?.Invoke();
                    }

                    if (!_isShuttingDown)
                        StopGameLoop(true);

                    break;
                }

                if (cancellationToken.IsCancellationRequested || _isShuttingDown)
                    break;

                // Wait for the interval before continuing
                var delay = _loopInterval;
                memoryEfficientLoop = false;

                if (memoryEfficientLoop)
                {
                    var currentCpuUsage = SystemMonitorHelper.GetCurrentCpuUsage();
                    var _maxCpuUsage = SystemMonitorHelper.GetMaxCpuUsage();

                    if (currentCpuUsage > _maxCpuUsage)
                    {
                        delay += (int)(currentCpuUsage - _maxCpuUsage) * 10;
                    }
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                if (cancellationToken.IsCancellationRequested || _isShuttingDown) break;

            }
        }

        private static async Task GameConnectionLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !_isShuttingDown)
            {
                if (gameConnected)
                {
                    break;
                }
                else
                {
                    ConnectFFXIVClient();
                }

                if (cancellationToken.IsCancellationRequested || _isShuttingDown)
                    break;

                var delay = _connectionAttempts >= _fastAttemptThreshold
                    ? _connectionIntervalSlow
                    : _connectionInterval;

                await Task.Delay(delay, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                if (cancellationToken.IsCancellationRequested || _isShuttingDown) break;
            }
        }

        private static bool IsGameRunning()
        {
            var processes = Process.GetProcessesByName("ffxiv_dx11");
            try
            {
                return processes.Length > 0;
            }
            finally
            {
                foreach (var p in processes) p.Dispose();
            }
        }

        private static void ConnectFFXIVClient()
        {
            try
            {
                if (_isShuttingDown) return;

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
                    // Dispose any extra Process handles we won't use. processes[0] is
                    // handed off to Sharlayan below and kept alive for the session.
                    for (int i = 1; i < processes.Length; i++) processes[i].Dispose();

                    var process = processes[0];
                    var processModel = new ProcessModel
                    {
                        Process = process
                    };

                    _configuration = new SharlayanConfiguration
                    {
                        ProcessModel = processModel,
                        GameLanguage = GameLanguage.English,
                    };

                    Debug.WriteLine($"Using Local Cache: {AppSettings.GetSettings().localcache}");

                    try
                    {
                        _memoryHandler = SharlayanMemoryManager.Instance.AddHandler(_configuration);
                    }
                    catch (Exception ex)
                    {
                        // Sharlayan throws Win32Exception(5) / UnauthorizedAccessException
                        // when opening FFXIV's process memory without admin rights.
                        // Normally AdminElevationHelper relaunches us as admin, but
                        // that path is skipped under a debugger. Swallow and keep
                        // trying so the UI/effects still work.
                        if (_connectionAttempts <= 1)
                            Logger.WriteConsole(LoggerTypes.FFXIV,
                                $"Cannot attach to FFXIV memory: {ex.Message}. Run Chromatics as Administrator to enable game-state effects.");
                        return;
                    }

                    gameConnected = true;
                    activeProcessId = _configuration.ProcessModel.ProcessID;

                }

                if (gameConnected)
                {
                    Logger.WriteConsole(LoggerTypes.FFXIV, @"Attached to FFXIV.");
                    _connectionAttempts = 0;

                    SafeCancel(_GameConnectionCancellationTokenSource);
                    RGBController.StopEffects(gameFirstConnected: true);
                    RGBController.ResetLayerGroups();
                    StartGameLoop();

                    Debug.WriteLine(@"Scanning memory..");
#if DEBUG
                    Thread.Sleep(1000);
                    foreach (var location in _memoryHandler.Scanner.Locations)
                    {
                        Debug.WriteLine($"Found {location.Key}. Location: {location.Value.GetAddress().ToInt64():X}");
                    }
#endif

                    GC.Collect();
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
            if (!gameConnected || _isShuttingDown) return;

            // Snapshot the handler reference so Dispose() on another thread can't
            // null it between our own reads below. The broad catch at the end is
            // still kept as a belt-and-braces safety net.
            var handler = _memoryHandler;
            if (handler == null) return;

            try
            {
                //Check if game has logged in

                if (handler.Reader != null && handler.Reader.CanGetActors() && handler.Reader.CanGetChatLog())
                {
                    var getCurrentPlayer = handler.Reader.GetCurrentPlayer();
                    var chatLogCount = handler.Reader.GetChatLog().ChatLogItems.Count;

                    var runningEffects = RGBController.GetRunningEffects();

                    if (getCurrentPlayer.Entity == null && chatLogCount <= 0 && !handler.Reader.GetGameState().IsLoggedIn)
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
                                    var ledgroup = new ListLedGroup(surface, device);

                                    var starfield = new StarfieldDecorator(ledgroup, (ledgroup.Count() / 4), 10, 500, highlightColors, surface, false, baseColor);
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

                            _layerProcessorFactory.DisposeAll();
                            GC.Collect();

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
                            GC.Collect();
                        }

                    }

                }

                if (!_isInGame) return;

                //Event Delegates
                if (handler.Reader != null && handler.Reader.CanGetActors())
                {
                    var getCurrentPlayer = handler.Reader.GetCurrentPlayer();
                    if (getCurrentPlayer.Entity != null)
                    {
                        if (getCurrentPlayer.Entity.Job != _currentJob)
                        {
                            jobChanged?.Invoke();
                            _currentJob = getCurrentPlayer.Entity.Job;
                        }
                    }
                }

                //Process All Layers
                var _layers = MappingLayers.GetLayers();

                foreach (IMappingLayer layer in _layers.Values.OrderBy(x => x.zindex, comparer))
                {
                    switch (layer.rootLayerType)
                    {
                        case LayerType.BaseLayer:
                            var baseProcessor = _layerProcessorFactory.GetProcessor((BaseLayerType)layer.layerTypeindex);
                            if (layer.requestUpdate)
                            {
                                var liveGroups = RGBController.GetLiveLayerGroups();
                                if (liveGroups.TryGetValue(layer.layerID, out var prevBaseGroups))
                                {
                                    foreach (var g in prevBaseGroups)
                                    {
                                        g?.RemoveAllDecorators();
                                        g?.Detach();
                                    }
                                    liveGroups.Remove(layer.layerID);
                                }
                            }
                            baseProcessor.Process(layer);
                            // Raid effect overlay runs on every base layer regardless of
                            // its subtype. The processor manages its own ListLedGroup at a
                            // higher ZIndex so it visibly overrides whichever base layer
                            // the user has chosen when the conditions are met.
                            Chromatics.Layers.RaidEffectProcessor.Instance.Process(layer);
                            break;

                        case LayerType.DynamicLayer:
                            var dynamicProcessor = _layerProcessorFactory.GetProcessor((DynamicLayerType)layer.layerTypeindex);
                            if (layer.requestUpdate)
                            {
                                // Generically detach whatever groups the previous processor
                                // registered for this layerID so they don't persist on the
                                // surface after a type switch.
                                var liveGroups = RGBController.GetLiveLayerGroups();
                                if (liveGroups.TryGetValue(layer.layerID, out var prevGroups))
                                {
                                    foreach (var g in prevGroups)
                                        g?.Detach();
                                    liveGroups.Remove(layer.layerID);
                                }
                                // Per-processor model cleanup (overridden on JobGaugeA/B/C).
                                foreach (var p in _layerProcessorFactory.GetActiveDynamicProcessors())
                                    if (p != dynamicProcessor)
                                        p.CleanupLayer(layer.layerID);
                            }
                            dynamicProcessor.Process(layer);
                            // Raid highlight overlay runs only for highlight-class dynamic
                            // layers so it overrides Highlight, JobClassesHighlight, and
                            // ReactiveWeatherHighlight on the user's selected keys. Other
                            // dynamic types (gauges, trackers, castbars) are left alone —
                            // overlaying them would clobber meaningful gameplay data.
                            var dynamicType = (DynamicLayerType)layer.layerTypeindex;
                            if (dynamicType == DynamicLayerType.Highlight ||
                                dynamicType == DynamicLayerType.JobClassesHighlight ||
                                dynamicType == DynamicLayerType.ReactiveWeatherHighlight)
                            {
                                Chromatics.Layers.RaidEffectHighlightProcessor.Instance.Process(layer);
                            }
                            else
                            {
                                Chromatics.Layers.RaidEffectHighlightProcessor.Instance.CleanupLayer(layer.layerID);
                            }
                            break;

                        case LayerType.EffectLayer:
                            var effectProcessors = EffectLayerProcessorFactory.GetProcessors();
                            foreach (var effectProcessor in effectProcessors)
                            {
                                effectProcessor.Value.Process(layer);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Debug.WriteLine is [Conditional("DEBUG")], so the call compiles
                // away in Release while still referencing `ex` for the analyzer.
                Debug.WriteLine($"Exception: {ex.Message}");
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
