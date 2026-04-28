using Chromatics.Core;
using Chromatics.Localization;
using RGB.NET.Core;
using NAudio.Wave;
using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class AudioVisualizerEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private List<Peak> peaks;
        private int columnsPerPeak = 2;
        private WasapiLoopbackCapture capture;
        private BufferedWaveProvider bufferedWaveProvider;
        private const int fftLength = 1024;
        private Complex[] fftBuffer = new Complex[fftLength];
        private readonly object _peakLock = new();
        private float[] _bandPeaks = [];

        private class Peak
        {
            public double CurrentHeight { get; set; }
            public double TargetHeight { get; set; }
            public int StartColumn { get; set; }
            public int EndColumn { get; set; }
        }

        public AudioVisualizerEffect(ListLedGroup _ledGroup, Color[] colors, RGBSurface surface, Color baseColor = default(Color)) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.colors = colors;
            this.baseColor = baseColor == default(Color) ? new Color(0, 0, 0) : baseColor;

            int maxCol;
            (_grid, _maxRow, maxCol) = DeviceGridHelper.GetGrid(_ledGroup);
            peaks = new List<Peak>();

            InitializePeaks();
            StartAudioCapture();
        }

        private void StartAudioCapture()
        {
            try
            {
                capture = new WasapiLoopbackCapture();
                capture.DataAvailable += OnDataAvailable;
                bufferedWaveProvider = new BufferedWaveProvider(capture.WaveFormat)
                {
                    DiscardOnBufferOverflow = true
                };
                capture.StartRecording();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AudioVisualizerEffect: Failed to start capture: {ex.Message}");
            }
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (bufferedWaveProvider == null) return;
            bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
            ProcessAudioBuffer();
        }

        private void InitializePeaks()
        {
            var cols = _grid.Values.Select(p => p[1]).Distinct().OrderBy(c => c).ToList();
            for (int i = 0; i < cols.Count; i += columnsPerPeak)
            {
                var startColumn = cols[i];
                var endColumn = (i + columnsPerPeak < cols.Count) ? cols[i + columnsPerPeak - 1] : cols[cols.Count - 1];
                peaks.Add(new Peak { CurrentHeight = 0, TargetHeight = 0, StartColumn = startColumn, EndColumn = endColumn });
            }
            _bandPeaks = new float[peaks.Count];
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            peaks.Clear();

            if (capture != null)
            {
                capture.DataAvailable -= OnDataAvailable;
                capture.StopRecording();
                capture.Dispose();
                capture = null;
            }

            bufferedWaveProvider = null;
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null || peaks == null || peaks.Count == 0) return;

                lock (_peakLock)
                {
                    foreach (var peak in peaks)
                    {
                        if (peak.TargetHeight > peak.CurrentHeight)
                            peak.CurrentHeight += (peak.TargetHeight - peak.CurrentHeight) * Math.Min(1.0, deltaTime * 10.0);
                        else
                            peak.CurrentHeight += (peak.TargetHeight - peak.CurrentHeight) * Math.Min(1.0, deltaTime * 5.0);
                    }
                }

                foreach (var led in ledGroup)
                {
                    if (_grid.TryGetValue(led.Id, out var position))
                    {
                        var row = position[0];
                        var col = position[1];
                        var peak = peaks.FirstOrDefault(p => col >= p.StartColumn && col <= p.EndColumn);
                        if (peak != null && peak.CurrentHeight > 0.1 && row >= (_maxRow - (int)Math.Round(peak.CurrentHeight)))
                        {
                            led.Color = GetColorForRow(row, (int)Math.Round(peak.CurrentHeight));
                        }
                        else
                        {
                            led.Color = baseColor;
                        }
                    }
                    else
                    {
                        led.Color = baseColor;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex.Message}");
            }
        }

        private void ProcessAudioBuffer()
        {
            if (bufferedWaveProvider == null || peaks == null || peaks.Count == 0) return;

            int available = bufferedWaveProvider.BufferedBytes;
            if (available == 0) return;

            var audioBytes = new byte[available];
            int bytesRead = bufferedWaveProvider.Read(audioBytes, 0, available);

            int bytesPerSample = bufferedWaveProvider.WaveFormat.BitsPerSample / 8;
            int channels = bufferedWaveProvider.WaveFormat.Channels;
            int totalSamples = bytesRead / bytesPerSample;

            // Mix down to mono by averaging channels
            int frameCount = totalSamples / channels;
            float[] monoSamples = new float[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                float sum = 0;
                for (int ch = 0; ch < channels; ch++)
                    sum += BitConverter.ToSingle(audioBytes, (f * channels + ch) * bytesPerSample);
                monoSamples[f] = sum / channels;
            }

            if (monoSamples.Length < fftLength)
                return;

            for (int i = 0; i < fftLength; i++)
            {
                float window = 0.54f - 0.46f * (float)Math.Cos(2 * Math.PI * i / (fftLength - 1));
                fftBuffer[i] = new Complex { X = monoSamples[i] * window, Y = 0 };
            }

            FastFourierTransform.FFT(true, (int)Math.Log(fftLength, 2.0), fftBuffer);

            int usableBins = fftLength / 2;
            float[] magnitudes = new float[usableBins];
            for (int i = 0; i < usableBins; i++)
                magnitudes[i] = (float)Math.Sqrt(fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y);

            // Logarithmic bin distribution with per-band normalization.
            // Each band uses average magnitude (not max) and its own running
            // peak tracker so quiet high-frequency bands still fill the display.
            int peakCount = peaks.Count;
            double logMin = Math.Log(1);
            double logMax = Math.Log(usableBins);

            lock (_peakLock)
            {
                for (int p = 0; p < peakCount; p++)
                {
                    int startBin = (int)Math.Exp(logMin + (logMax - logMin) * p / peakCount);
                    int endBin   = (int)Math.Exp(logMin + (logMax - logMin) * (p + 1) / peakCount);
                    startBin = Math.Max(1, Math.Min(startBin, usableBins - 1));
                    endBin   = Math.Max(startBin + 1, Math.Min(endBin, usableBins));

                    float bandSum = 0;
                    float bandMax = 0;
                    int count = 0;
                    for (int b = startBin; b < endBin; b++)
                    {
                        bandSum += magnitudes[b];
                        bandMax = Math.Max(bandMax, magnitudes[b]);
                        count++;
                    }

                    float bandLevel = count > 0 ? (bandSum / count * 0.5f + bandMax * 0.5f) : 0;

                    if (bandLevel > _bandPeaks[p])
                        _bandPeaks[p] = bandLevel;
                    else
                        _bandPeaks[p] *= 0.97f;

                    float normalizedLevel = _bandPeaks[p] > 0.001f ? bandLevel / _bandPeaks[p] : 0;

                    double height = Math.Min(Math.Pow(normalizedLevel, 0.9) * _maxRow * 0.85, _maxRow);
                    peaks[p].TargetHeight = height;
                }
            }
        }

        private Color GetColorForRow(int row, int peakHeight)
        {
            if (_maxRow == 0 || colors.Length <= 1) return colors[0];
            double normalized = (double)(row - (_maxRow - peakHeight)) / _maxRow;
            var colorIndex = (int)(normalized * (colors.Length - 1));
            return colors[Math.Max(0, Math.Min(colorIndex, colors.Length - 1))];
        }
    }
}
