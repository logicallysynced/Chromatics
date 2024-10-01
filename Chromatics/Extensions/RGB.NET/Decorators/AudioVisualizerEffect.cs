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
        private List<Peak> peaks;
        private int maxRow;
        private int columnsPerPeak = 2; // Number of columns per peak
        private WasapiLoopbackCapture capture;
        private BufferedWaveProvider bufferedWaveProvider;
        private const int fftLength = 1024; // NAudio FFT length
        private Complex[] fftBuffer = new Complex[fftLength];

        private class Peak
        {
            public int CurrentHeight { get; set; }
            public int TargetHeight { get; set; }
            public int StartColumn { get; set; }
            public int EndColumn { get; set; }
        }

        public AudioVisualizerEffect(ListLedGroup _ledGroup, Color[] colors, RGBSurface surface, Color baseColor = default(Color)) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.colors = colors;
            this.baseColor = baseColor == default(Color) ? new Color(0, 0, 0) : baseColor;

            peaks = new List<Peak>();

            InitializePeaks();
            StartAudioCapture();
        }

        private void StartAudioCapture()
        {
            capture = new WasapiLoopbackCapture();
            Debug.WriteLine($"Audio Format: {capture.WaveFormat}");
            capture.DataAvailable += OnDataAvailable;
            bufferedWaveProvider = new BufferedWaveProvider(capture.WaveFormat)
            {
                DiscardOnBufferOverflow = true
            };

            capture.StartRecording();
            Debug.WriteLine("Audio capture started.");
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            Debug.WriteLine($"Bytes recorded: {e.BytesRecorded}");
            bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
            ProcessAudioBuffer();
        }

        private void InitializePeaks()
        {
            var cols = KeyLocalization.QWERTY_Grid.Values.Select(p => p[1]).Distinct().OrderBy(c => c).ToList();
            for (int i = 0; i < cols.Count; i += columnsPerPeak)
            {
                var startColumn = cols[i];
                var endColumn = (i + columnsPerPeak < cols.Count) ? cols[i + columnsPerPeak - 1] : cols[cols.Count - 1];
                peaks.Add(new Peak { CurrentHeight = 0, TargetHeight = 0, StartColumn = startColumn, EndColumn = endColumn });
            }
            maxRow = KeyLocalization.QWERTY_Grid.Values.Max(p => p[0]);
            Debug.WriteLine($"Initialized {peaks.Count} peaks.");
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
            capture.StopRecording();
            capture.Dispose();
            Debug.WriteLine("Audio capture stopped.");
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null || peaks == null) return;

                foreach (var led in ledGroup)
                {
                    if (KeyLocalization.QWERTY_Grid.TryGetValue(led.Id, out var position))
                    {
                        var row = position[0];
                        var col = position[1];
                        var peak = peaks.FirstOrDefault(p => col >= p.StartColumn && col <= p.EndColumn);
                        if (peak != null && row >= (maxRow - peak.CurrentHeight))
                        {
                            led.Color = GetColorForRow(row, peak.CurrentHeight);
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
#if DEBUG
                Debug.WriteLine($"Exception: {ex.Message}");
#endif
            }
        }

        private void ProcessAudioBuffer()
        {
            var audioBytes = new byte[bufferedWaveProvider.BufferLength];
            int bytesRead = bufferedWaveProvider.Read(audioBytes, 0, audioBytes.Length);
            Debug.WriteLine($"Bytes read from buffer: {bytesRead}");

            int bytesPerSample = bufferedWaveProvider.WaveFormat.BitsPerSample / 8;
            int sampleCount = bytesRead / bytesPerSample;

            float[] audioSamples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                audioSamples[i] = BitConverter.ToSingle(audioBytes, i * bytesPerSample);
            }

            Debug.WriteLine($"Sample count: {sampleCount}");
            Debug.WriteLine($"First 10 audio samples: {string.Join(", ", audioSamples.Take(10))}");

            if (audioSamples.Length < fftLength)
                return;

            // Apply window function (Hamming)
            for (int i = 0; i < fftLength; i++)
            {
                float window = 0.54f - 0.46f * (float)Math.Cos(2 * Math.PI * i / (fftLength - 1));
                fftBuffer[i] = new Complex { X = i < audioSamples.Length ? audioSamples[i] * window : 0, Y = 0 };
            }

            // Perform FFT
            FastFourierTransform.FFT(true, (int)Math.Log(fftLength, 2.0), fftBuffer);

            float[] magnitudes = new float[fftLength / 2];
            for (int i = 0; i < fftLength / 2; i++)
            {
                magnitudes[i] = (float)Math.Sqrt(fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y);
            }

            Debug.WriteLine($"First 10 magnitudes: {string.Join(", ", magnitudes.Take(10))}");

            // Normalize and apply gain
            float maxMagnitude = magnitudes.Max();
            Debug.WriteLine($"Max magnitude before gain: {maxMagnitude}");

            float gain = 5.0f; // Reduced gain factor
            if (maxMagnitude > 0)
            {
                for (int i = 0; i < magnitudes.Length; i++)
                {
                    magnitudes[i] = (magnitudes[i] / maxMagnitude) * gain;
                }
            }

            Debug.WriteLine($"First 10 magnitudes after gain: {string.Join(", ", magnitudes.Take(10))}");

            // Determine peak height and clamp it
            var calculatedPeakHeight = (int)(magnitudes.Max() * maxRow / gain);
            calculatedPeakHeight = Math.Min(calculatedPeakHeight, maxRow); // Clamp the peak height to the max row value
            Debug.WriteLine($"Calculated peak height: {calculatedPeakHeight}");

            foreach (var peak in peaks)
            {
                peak.TargetHeight = calculatedPeakHeight;
                Debug.WriteLine($"Updated peak: StartColumn={peak.StartColumn}, EndColumn={peak.EndColumn}, TargetHeight={peak.TargetHeight}");
            }

            // Smoothly update current heights to target heights using linear interpolation
            foreach (var peak in peaks)
            {
                int previousHeight = peak.CurrentHeight;
                peak.CurrentHeight = LinearInterpolate(peak.CurrentHeight, peak.TargetHeight, 0.05); // Adjusted smoothing factor
                Debug.WriteLine($"Updated peak: StartColumn={peak.StartColumn}, EndColumn={peak.EndColumn}, CurrentHeight={peak.CurrentHeight}, PreviousHeight={previousHeight}, TargetHeight={peak.TargetHeight}");
            }

            Debug.WriteLine($"Max magnitude after gain: {magnitudes.Max()}");
        }

        private int LinearInterpolate(int start, int end, double factor)
        {
            return (int)(start + factor * (end - start));
        }

        private Color GetColorForRow(int row, int peakHeight)
        {
            var colorIndex = (int)((double)(row - (maxRow - peakHeight)) / maxRow * (colors.Length - 1));
            return colors[Math.Max(0, Math.Min(colorIndex, colors.Length - 1))];
        }
    }
}
